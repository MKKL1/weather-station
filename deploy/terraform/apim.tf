resource "azurerm_api_management" "api_gateway" {
  name                = "apim-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.project_scope.location
  resource_group_name = azurerm_resource_group.project_scope.name
  publisher_name      = var.apim_publisher_name
  publisher_email     = var.apim_publisher_email
  sku_name            = "Consumption_0"

  identity {
    type = "SystemAssigned"
  }
  tags = var.tags
}

resource "azurerm_api_management_named_value" "function_app_main_client_id" {
  name                = "function-app-main-client-id"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  display_name        = "function-app-main-client-id"
  value               = azuread_application.function_app_main_auth.client_id
  secret              = false
}

resource "azurerm_api_management_named_value" "access_token_jwt_public_key" {
  name                = "access-token-jwt-public-key"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  display_name        = "access-token-jwt-public-key"
  value               = var.access_token_public_key
  secret              = false
}

resource "azurerm_api_management_named_value" "function_app_client_id" {
  name                = "function-app-client-id"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  display_name        = "function-app-client-id"
  value               = azuread_application.function_app_auth.client_id
  secret              = false
}

resource "azurerm_api_management_named_value" "access_token_issuer_url" {
  name                = "access_token_issuer_url"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  display_name        = "access_token_issuer_url"
  value               = "https://${azurerm_static_web_app.public_keys_host.default_host_name}"
  secret              = false
}

resource "azurerm_api_management_backend" "auth_backend" {
  name                = "auth-backend"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.provisioning_function.default_hostname}/api/v1"
}

resource "azurerm_api_management_backend" "telemetry_backend" {
  name                = "telemetry-backend"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.main_function.default_hostname}/api/v1"
}

resource "azurerm_api_management_api" "provisioning_api" {
  name                  = "provisioning-api"
  resource_group_name   = azurerm_resource_group.project_scope.name
  api_management_name   = azurerm_api_management.api_gateway.name
  revision              = "1"
  display_name          = "Device Provisioning API"
  path                  = "provisioning"
  protocols             = ["https"]
  subscription_required = false

  service_url = "https://${azurerm_function_app_flex_consumption.provisioning_function.default_hostname}"
}

resource "azurerm_api_management_api_operation" "device_register" {
  operation_id        = "device-register"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Register device"
  method              = "POST"
  url_template        = "/register"
}

resource "azurerm_api_management_api_operation_policy" "device_register_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.device_register.operation_id

  xml_content = file("${path.module}/policies/device_register.xml")
  depends_on  = [azurerm_api_management_named_value.access_token_issuer_url, null_resource.swa_deploy]
}

resource "azurerm_api_management_api" "device_api" {
  name                  = "device-api"
  resource_group_name   = azurerm_resource_group.project_scope.name
  api_management_name   = azurerm_api_management.api_gateway.name
  revision              = "1"
  display_name          = "Device API"
  path                  = "device"
  protocols             = ["https"]
  subscription_required = false
}

resource "azurerm_api_management_api_operation" "token_refresh" {
  operation_id        = "token-refresh"
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Token Refresh"
  method              = "POST"
  url_template        = "/auth/token"
}

resource "azurerm_api_management_api_operation_policy" "token_refresh_policy" {
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.token_refresh.operation_id

  xml_content = file("${path.module}/policies/token_refresh.xml")
  depends_on  = [azurerm_api_management_named_value.access_token_issuer_url, null_resource.swa_deploy]
}

resource "azurerm_api_management_api_operation" "telemetry" {
  operation_id        = "telemetry"
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Send Telemetry"
  method              = "POST"
  url_template        = "/telemetry"
}

resource "azurerm_api_management_api_operation_policy" "telemetry_policy" {
  api_name            = azurerm_api_management_api.device_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.telemetry.operation_id

  xml_content = file("${path.module}/policies/telemetry.xml")
  depends_on  = [azurerm_api_management_named_value.access_token_issuer_url, null_resource.swa_deploy]
}