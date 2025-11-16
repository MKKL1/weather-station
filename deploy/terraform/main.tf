# This resource is used to generate a unique suffix for resources
# to avoid naming conflicts
resource "random_id" "suffix" {
  byte_length = 4
}

locals {
  # Clean project and env names for storage account (alphanumeric, lowercase)
  sa_project_name = lower(replace(var.project_name, "/[^a-zA-Z0-9]/", ""))
  sa_env_name     = lower(replace(var.environment, "/[^a-zA-Z0-9]/", ""))

  # Random suffix for all resources
  random_suffix = random_id.suffix.hex

  # Construct a unique name, ensuring it fits length constraints (max 24)
  # "st" + "project" + "env" + "random"
  # We'll take max 10 from project, 3 from env, plus "st" (2) and random (8) = 23
  storage_account_name = var.storage_account_name != "" ? var.storage_account_name : "st${substr(local.sa_project_name, 0, 10)}${substr(local.sa_env_name, 0, 3)}${local.random_suffix}"
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = var.location
}

resource "azurerm_storage_account" "sa" {
  name                     = local.storage_account_name # Use the generated unique name
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_service_plan" "flex_asp" {
  name                = "asp-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "FC1"
}

resource "azurerm_cosmosdb_account" "this" {
  name                = "cosmos-${var.project_name}-${var.environment}-${local.random_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }

  capabilities {
    name = "EnableServerless"
  }
}

resource "azurerm_cosmosdb_sql_database" "this" {
  name                = "${var.project_name}-db"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
}

resource "azurerm_cosmosdb_sql_container" "this" {
  name                = "${var.project_name}-container"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
  database_name       = azurerm_cosmosdb_sql_database.this.name

  partition_key_paths = ["/deviceId"]
}

resource "azurerm_cosmosdb_sql_container" "views" {
  name                = "${var.project_name}-views"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
  database_name       = azurerm_cosmosdb_sql_database.this.name

  partition_key_paths = ["/deviceId"]
}

resource "azurerm_storage_container" "function_app_container" {
  name                  = var.function_app_container_name
  storage_account_id    = azurerm_storage_account.sa.id
  container_access_type = "private"
}

resource "azurerm_log_analytics_workspace" "logs" {
  name                = "log-${var.project_name}-${var.environment}-${local.random_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "appinsights" {
  name                = "appi-${var.project_name}-${var.environment}-${local.random_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  workspace_id        = azurerm_log_analytics_workspace.logs.id
  application_type    = "web"
}

resource "azurerm_function_app_flex_consumption" "function_app" {
  name                = "fn-${var.project_name}-${var.environment}-${local.random_suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  service_plan_id     = azurerm_service_plan.flex_asp.id

  storage_container_endpoint  = "${azurerm_storage_account.sa.primary_blob_endpoint}${azurerm_storage_container.function_app_container.name}"
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.sa.primary_access_key

  runtime_name    = "dotnet-isolated"
  runtime_version = "8.0"

  maximum_instance_count = 40
  instance_memory_in_mb  = 512

  site_config {} # Required, even if empty

  app_settings = {
    AzureWebJobsStorage = azurerm_storage_account.sa.primary_connection_string
    COSMOS_CONNECTION      = azurerm_cosmosdb_account.this.primary_sql_connection_string
    COSMOS_DATABASE        = azurerm_cosmosdb_sql_database.this.name
    COSMOS_CONTAINER       = azurerm_cosmosdb_sql_container.this.name
    COSMOS_VIEWS_CONTAINER = azurerm_cosmosdb_sql_container.views.name

    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.appinsights.connection_string
    ApplicationInsightsAgent_EXTENSION_VERSION = "~3"
  }

  depends_on = [azurerm_storage_container.function_app_container]
}

resource "azurerm_service_plan" "flex_asp_provisioning" {
  name                = "asp-${var.project_name}-${var.environment}-provisioning"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "FC1"
}

resource "azurerm_storage_container" "function_app_provisioning_container" {
  name                  = "${var.function_app_container_name}-provisioning"  # or use a separate variable
  storage_account_id    = azurerm_storage_account.sa.id
  container_access_type = "private"
}

resource "azurerm_function_app_flex_consumption" "function_app_provisioning" {
  name                = "fn-${var.project_name}-${var.environment}-provisioning-${local.random_suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  service_plan_id     = azurerm_service_plan.flex_asp_provisioning.id

  storage_container_endpoint  = "${azurerm_storage_account.sa.primary_blob_endpoint}${azurerm_storage_container.function_app_provisioning_container.name}"
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.sa.primary_access_key

  runtime_name    = "custom"
  runtime_version = "1.0"

  maximum_instance_count = 40
  instance_memory_in_mb  = 512

  identity {
    type = "SystemAssigned"
  }

  site_config {}

  auth_settings {
    enabled                       = true
    unauthenticated_client_action = "RedirectToLoginPage"
    default_provider              = "AzureActiveDirectory"
    
    active_directory {
      client_id = azuread_application.function_app_auth.client_id
      allowed_audiences = [
        azuread_application.function_app_auth.client_id,
        "api://${azuread_application.function_app_auth.client_id}"
      ]
  }
  }

  app_settings = {
    AzureWebJobsStorage      = azurerm_storage_account.sa.primary_connection_string
    COSMOS_CONNECTION        = azurerm_cosmosdb_account.this.primary_sql_connection_string
    COSMOS_DATABASE          = azurerm_cosmosdb_sql_database.this.name
    COSMOS_CONTAINER         = "device-registry"
    ACCESS_TOKEN_PRIVATE_KEY = var.access_token_private_key
    WEBSITE_AUTH_AAD_ALLOWED_TENANTS = data.azurerm_client_config.current.tenant_id
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.appinsights.connection_string
    ApplicationInsightsAgent_EXTENSION_VERSION = "~3"
  }

  depends_on = [
    azurerm_storage_container.function_app_provisioning_container,
    azurerm_api_management.apim,
    azuread_application.function_app_auth,
    azuread_application_identifier_uri.function_app_auth
  ]
}

data "azurerm_client_config" "current" {}

resource "azuread_application" "function_app_auth" {
  display_name = "fn-${var.project_name}-${var.environment}-provisioning-auth"
  
  sign_in_audience = "AzureADMyOrg"
  
  api {
    requested_access_token_version = 2
  }

  app_role {
    allowed_member_types = ["Application"]
    description          = "Allow APIM to call the Function App"
    display_name         = "APIM.Access"
    enabled              = true
    id                   = "00000000-0000-0000-0000-000000000001"
    value                = "APIM.Access"
  }
}

resource "azuread_app_role_assignment" "apim_to_function_app" {
  app_role_id         = "00000000-0000-0000-0000-000000000001"
  principal_object_id = azurerm_api_management.apim.identity[0].principal_id
  resource_object_id  = azuread_service_principal.function_app_auth.object_id

  depends_on = [
    azurerm_api_management.apim,
    azuread_service_principal.function_app_auth
  ]
}

resource "azuread_application_identifier_uri" "function_app_auth" {
  application_id = azuread_application.function_app_auth.id
  identifier_uri = "api://${azuread_application.function_app_auth.client_id}"
}

resource "azuread_service_principal" "function_app_auth" {
  client_id = azuread_application.function_app_auth.client_id
  
  app_role_assignment_required = true
  
  depends_on = [azuread_application_identifier_uri.function_app_auth]
}

resource "azurerm_api_management" "apim" {
  name                = "apim-${var.project_name}-${var.environment}-${local.random_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  publisher_name      = var.apim_publisher_name
  publisher_email     = var.apim_publisher_email

  sku_name = "Consumption_0"

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_api_management_backend" "auth_backend" {
  name                = "auth-backend"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.function_app_provisioning.default_hostname}/api/v1"
}

resource "azurerm_api_management_backend" "telemetry_backend" {
  name                = "telemetry-backend"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.function_app.default_hostname}"
}

resource "azurerm_api_management_certificate" "provisioning_cert" {
  name                = "provisioning-public-key"
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  data                = var.provisioning_public_key
}

resource "azurerm_api_management_named_value" "access_token_jwt_public_key" {
  name                = "access-token-jwt-public-key"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  display_name        = "access-token-jwt-public-key"
  value               = var.access_token_public_key
  secret              = false
}

# Store the Function App's App Registration client ID as a named value
resource "azurerm_api_management_named_value" "function_app_client_id" {
  name                = "function-app-client-id"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  display_name        = "function-app-client-id"
  value               = azuread_application.function_app_auth.client_id
  secret              = false
}

resource "azurerm_api_management_api" "provisioning_api" {
  name                  = "provisioning-api"
  resource_group_name   = azurerm_resource_group.rg.name
  api_management_name   = azurerm_api_management.apim.name
  revision              = "1"
  display_name          = "Device Provisioning API"
  path                  = "provisioning"
  protocols             = ["https"]
  subscription_required = false

  service_url = "https://${azurerm_function_app_flex_consumption.function_app_provisioning.default_hostname}"
}

resource "azurerm_api_management_api_operation" "device_register" {
  operation_id        = "device-register"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  display_name        = "Register device"
  method              = "POST"
  url_template        = "/register"
  description         = "Exchange provisioning JWT for permanent HMAC secret"
}

resource "azurerm_api_management_api_operation_policy" "device_register_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  operation_id        = azurerm_api_management_api_operation.device_register.operation_id

  xml_content = <<XML
<policies>
  <inbound>
    <base />
    <!-- Validate provisioning JWT from device -->
    <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Invalid or expired provisioning token" require-expiration-time="false">
      <issuer-signing-keys>
        <key certificate-id="provisioning-public-key" />
      </issuer-signing-keys>
      <required-claims>
        <claim name="sub" match="any" />
      </required-claims>
    </validate-jwt>
    
    <!-- Extract device ID from JWT sub claim and store in variable -->
    <set-variable name="device-id" value="@{
      var jwt = context.Request.Headers.GetValueOrDefault("Authorization", "").Split(' ').Last();
      Jwt token;
      if (jwt.TryParseJwt(out token)) {
        return token.Claims.GetValueOrDefault("sub", "unknown");
      }
      return "unknown";
    }" />
    
    <!-- Get managed identity token for Function App authentication -->
    <authentication-managed-identity resource="{{function-app-client-id}}" output-token-variable-name="msi-access-token" ignore-error="false" />

    <!-- Replace Authorization header with managed identity token -->
    <set-header name="Authorization" exists-action="override">
      <value>@("Bearer " + (string)context.Variables["msi-access-token"])</value>
    </set-header>
    
    <!-- Set device ID header from validated JWT -->
    <set-header name="X-Device-ID" exists-action="override">
      <value>@((string)context.Variables["device-id"])</value>
    </set-header>
    
    <set-backend-service backend-id="auth-backend" />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
XML
}

resource "azurerm_api_management_api" "device_api" {
  name                  = "device-api"
  resource_group_name   = azurerm_resource_group.rg.name
  api_management_name   = azurerm_api_management.apim.name
  revision              = "1"
  display_name          = "Device API"
  path                  = "device"
  protocols             = ["https"]
  subscription_required = false
}

resource "azurerm_api_management_api_operation" "token_refresh" {
  operation_id        = "token-refresh"
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  display_name        = "Token Refresh"
  method              = "POST"
  url_template        = "/auth/token"
  description         = "Exchange HMAC signature for short-lived JWT"
}

resource "azurerm_api_management_api_operation_policy" "token_refresh_policy" {
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  operation_id        = azurerm_api_management_api_operation.token_refresh.operation_id

  xml_content = <<XML
<policies>
  <inbound>
    <base />
    <!-- Validate provisioning JWT to avoid spam-->
    <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Invalid or expired provisioning token" require-expiration-time="false">
      <issuer-signing-keys>
        <key certificate-id="provisioning-public-key" />
      </issuer-signing-keys>
      <required-claims>
        <claim name="sub" match="any" />
      </required-claims>
    </validate-jwt>
    
    <!-- Extract device ID from JWT sub claim and store in variable -->
    <set-variable name="device-id" value="@{
      var jwt = context.Request.Headers.GetValueOrDefault("Authorization", "").Split(' ').Last();
      Jwt token;
      if (jwt.TryParseJwt(out token)) {
        return token.Claims.GetValueOrDefault("sub", "unknown");
      }
      return "unknown";
    }" />
    
    <!-- Get managed identity token for Function App authentication -->
    <authentication-managed-identity resource="{{function-app-client-id}}" output-token-variable-name="msi-access-token" ignore-error="false" />
    
    <!-- Replace Authorization header with managed identity token -->
    <set-header name="Authorization" exists-action="override">
      <value>@("Bearer " + (string)context.Variables["msi-access-token"])</value>
    </set-header>
    
    <!-- Set device ID header from validated JWT -->
    <set-header name="X-Device-ID" exists-action="override">
      <value>@((string)context.Variables["device-id"])</value>
    </set-header>
    
    <set-backend-service backend-id="auth-backend" />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
XML
}

resource "azurerm_api_management_api_operation" "telemetry" {
  operation_id        = "telemetry"
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  display_name        = "Send Telemetry"
  method              = "POST"
  url_template        = "/telemetry"
  description         = "Send telemetry data with short-lived JWT"
}

resource "azurerm_api_management_api_operation_policy" "telemetry_policy" {
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  operation_id        = azurerm_api_management_api_operation.telemetry.operation_id

  xml_content = <<XML
<policies>
  <inbound>
    <base />
    <!-- Validate access token JWT -->
    <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Invalid or expired access token" require-scheme="Bearer">
      <openid-config url="{{access_token_issuer_url}}/.well-known/openid-configuration" />
      <audiences>
        <audience>weather-api</audience>
      </audiences>
      <issuers>
        <issuer>{{access_token_issuer_url}}</issuer>
      </issuers>
      <required-claims>
        <claim name="sub" match="any" />
        <claim name="roles" match="any">
          <value>weather-telemetry-write</value>
        </claim>
      </required-claims>
    </validate-jwt>
    
    <!-- Extract device ID from JWT sub claim -->
    <set-header name="X-Device-ID" exists-action="override">
      <value>@{
        var jwt = context.Request.Headers.GetValueOrDefault("Authorization", "").Split(' ').Last();
        Jwt token;
        if (jwt.TryParseJwt(out token)) {
          return token.Claims.GetValueOrDefault("sub", "unknown");
        }
        return "unknown";
      }</value>
    </set-header>
    
    <set-backend-service backend-id="telemetry-backend" />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
XML

depends_on = [ azurerm_api_management_named_value.access_token_issuer_url ]
}



resource "azurerm_static_web_app" "jwks" {
  name                = "swa-${var.project_name}-${var.environment}-jwks-${local.random_suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = "eastus2"
  sku_tier            = "Free"
  sku_size            = "Free"
}

resource "azurerm_static_web_app_custom_domain" "jwks_domain" {
  count               = var.custom_jwks_domain != "" ? 1 : 0
  static_web_app_id   = azurerm_static_web_app.jwks.id
  domain_name         = var.custom_jwks_domain
  validation_type     = "cname-delegation"
}

resource "azurerm_api_management_named_value" "access_token_issuer_url" {
  name                = "access_token_issuer_url"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  display_name        = "access_token_issuer_url"
  value               = "https://${azurerm_static_web_app.jwks.default_host_name}"
  secret              = false
}