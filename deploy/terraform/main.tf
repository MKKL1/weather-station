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

# Update the provisioning function app to use the new service plan
resource "azurerm_function_app_flex_consumption" "function_app_provisioning" {
  name                = "fn-${var.project_name}-${var.environment}-provisioning-${local.random_suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  service_plan_id     = azurerm_service_plan.flex_asp_provisioning.id  # Changed from flex_asp to flex_asp_provisioning

  storage_container_endpoint  = "${azurerm_storage_account.sa.primary_blob_endpoint}${azurerm_storage_container.function_app_provisioning_container.name}"
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.sa.primary_access_key

  runtime_name    = "custom"
  runtime_version = "1.0"

  maximum_instance_count = 40
  instance_memory_in_mb  = 512

  site_config {}

  app_settings = {
    AzureWebJobsStorage = azurerm_storage_account.sa.primary_connection_string
    COSMOS_CONNECTION = azurerm_cosmosdb_account.this.primary_sql_connection_string
    COSMOS_DATABASE = azurerm_cosmosdb_sql_database.this.name
    COSMOS_CONTAINER = "device-registry"
  }

  depends_on = [azurerm_storage_container.function_app_provisioning_container]
}

resource "azurerm_api_management" "apim" {
  name                = "apim-${var.project_name}-${var.environment}-${local.random_suffix}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  publisher_name      = var.apim_publisher_name
  publisher_email     = var.apim_publisher_email

  sku_name = "Consumption_0"
}

resource "azurerm_api_management_certificate" "ca_cert" {
  name                = "provisioning-ca-cert"
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  data = filebase64(var.ca_cert_path)
}

resource "azurerm_api_management_backend" "function_app_backend" {
  name                = "provisioning-service-backend"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.function_app_provisioning.default_hostname}"
}

resource "azurerm_api_management_api" "provisioning_api" {
  name                = "provisioning-api"
  resource_group_name = azurerm_resource_group.rg.name
  api_management_name = azurerm_api_management.apim.name
  revision            = "1"
  display_name        = "Provisioning Service API"
  path                = "provisioning"  # Changed from "/" to a valid path
  protocols           = ["https"]
}

# Device Activate Operation
resource "azurerm_api_management_api_operation" "device_activate" {
  operation_id        = "device-activate"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  display_name        = "Device Activate"
  method              = "POST"
  url_template        = "/device/activate"
}

# User Claim Operation
# resource "azurerm_api_management_api_operation" "user_claim" {
#   operation_id        = "user-claim"
#   api_name            = azurerm_api_management_api.provisioning_api.name
#   api_management_name = azurerm_api_management.apim.name
#   resource_group_name = azurerm_resource_group.rg.name
#   display_name        = "User Claim"
#   method              = "POST"
#   url_template        = "/user/claim"
# }

# Policy for Device Endpoints (mTLS validation)
resource "azurerm_api_management_api_operation_policy" "device_activate_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.apim.name
  resource_group_name = azurerm_resource_group.rg.name
  operation_id        = azurerm_api_management_api_operation.device_activate.operation_id

  xml_content = <<XML
<policies>
  <inbound>
    <base />
    <!-- Require client certificate -->
    <choose>
      <when condition="@(context.Request.Certificate == null)">
        <return-response>
          <set-status code="401" reason="Unauthorized" />
          <set-body>Client certificate required</set-body>
        </return-response>
      </when>
    </choose>
    
    <!-- Validate certificate against CA -->
    <choose>
      <when condition="@(!context.Request.Certificate.Verify())">
        <return-response>
          <set-status code="403" reason="Forbidden" />
          <set-body>Certificate validation failed</set-body>
        </return-response>
      </when>
    </choose>
    
    <!-- Extract device ID from certificate CN and add header -->
    <set-header name="X-Device-ID" exists-action="override">
      <value>@{
        var cert = context.Request.Certificate;
        if (cert != null && !string.IsNullOrEmpty(cert.SubjectName.Name))
        {
          var parts = cert.SubjectName.Name.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
          foreach (var part in parts)
          {
            var keyValue = part.Trim().Split('=');
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals("CN", StringComparison.OrdinalIgnoreCase))
            {
              return keyValue[1].Trim();
            }
          }
        }
        return "unknown";
      }</value>
    </set-header>
    
    <!-- Rate limiting (Consumption SKU only supports basic rate-limit) -->
    <rate-limit calls="100" renewal-period="60" />
    
    <!-- Set backend -->
    <set-backend-service backend-id="provisioning-service-backend" />
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

# Policy for User Endpoints (JWT validation)
# resource "azurerm_api_management_api_operation_policy" "user_claim_policy" {
#   api_name            = azurerm_api_management_api.provisioning_api.name
#   api_management_name = azurerm_api_management.apim.name
#   resource_group_name = azurerm_resource_group.rg.name
#   operation_id        = azurerm_api_management_api_operation.user_claim.operation_id

#   xml_content = <<XML
# <policies>
#   <inbound>
#     <base />
#     <!-- Validate JWT token -->
#     <validate-jwt header-name="Authorization" failed-validation-httpcode="401">
#       <openid-config url="${var.jwt_issuer}/.well-known/openid-configuration" />
#       <audiences>
#         <audience>${var.jwt_audience}</audience>
#       </audiences>
#       <required-claims>
#         <claim name="sub" match="any">
#           <value>@(context.Request.Headers.GetValueOrDefault("Authorization","").Split(' ')[1])</value>
#         </claim>
#       </required-claims>
#     </validate-jwt>
    
#     <!-- Extract user ID from JWT and add header -->
#     <set-header name="X-User-ID" exists-action="override">
#       <value>@{
#         string token = context.Request.Headers.GetValueOrDefault("Authorization","").Split(' ')[1];
#         Jwt jwt;
#         if (token.TryParseJwt(out jwt)) {
#           return jwt.Claims.GetValueOrDefault("sub", "");
#         }
#         return "";
#       }</value>
#     </set-header>
    
#     <!-- Rate limiting per user -->
#     <rate-limit-by-key calls="20" 
#                        renewal-period="3600" 
#                        counter-key="@(context.Request.Headers.GetValueOrDefault("X-User-ID"))" />
#   </inbound>
#   <backend>
#     <base />
#   </backend>
#   <outbound>
#     <base />
#   </outbound>
#   <on-error>
#     <base />
#   </on-error>
# </policies>
# XML
# }