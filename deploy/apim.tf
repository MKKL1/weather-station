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

resource "azurerm_api_management_named_value" "tenant_id" {
  name                = "tenant-id"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  display_name        = "tenant-id"
  value               = data.azurerm_client_config.current.tenant_id
  secret              = false
}

resource "azurerm_api_management_named_value" "external_worker_client_id" {
  name                = "external-worker-client-id"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  display_name        = "external-worker-client-id"
  value               = azuread_application.external_worker_app.client_id
  secret              = false
}

resource "azurerm_api_management_policy_fragment" "provisioning_jwt" {
  api_management_id = azurerm_api_management.api_gateway.id
  name              = "provisioning-jwt"
  format            = "rawxml"
  value             = file("${path.module}/policies/fragments/provisioning_jwt.xml")

  depends_on = [
    azurerm_api_management_named_value.access_token_issuer_url,
    azurerm_api_management_named_value.function_app_client_id
  ]
}



// ===============================================
// Provisioning
// ===============================================

resource "azurerm_api_management_backend" "auth_backend" {
  name                = "auth-backend"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.provisioning_function.default_hostname}/api/v1"
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

// /provisioning/{deviceId}/register

resource "azurerm_api_management_api_operation" "device_register" {
  operation_id        = "device-register"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Register device"
  method              = "POST"
  url_template        = "/{deviceId}/register"

  template_parameter {
    name     = "deviceId"
    required = true
    type     = "string"
  }
}

resource "azurerm_api_management_api_operation_policy" "device_register_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.device_register.operation_id

  xml_content = file("${path.module}/policies/device_register.xml")
  depends_on  = [azurerm_api_management_policy_fragment.provisioning_jwt, null_resource.swa_deploy]
}

// /provisioning/{deviceId}/token

resource "azurerm_api_management_api_operation" "token_refresh" {
  operation_id        = "token-refresh"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Token Refresh"
  method              = "POST"
  url_template        = "/{deviceId}/token"

  template_parameter {
    name     = "deviceId"
    required = true
    type     = "string"
  }
}

resource "azurerm_api_management_api_operation_policy" "token_refresh_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.token_refresh.operation_id

  xml_content = file("${path.module}/policies/token_refresh.xml")
  depends_on  = [azurerm_api_management_policy_fragment.provisioning_jwt, null_resource.swa_deploy]
}

// /provisioning/{deviceId}/claim-code

resource "azurerm_api_management_api_operation" "claim_code" {
  operation_id        = "claim-code"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Generate claim code"
  method              = "POST"
  url_template        = "/{deviceId}/claim-code"

  template_parameter {
    name     = "deviceId"
    required = true
    type     = "string"
  }
}

resource "azurerm_api_management_api_operation_policy" "claim_code_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.claim_code.operation_id

  xml_content = file("${path.module}/policies/claim_code.xml")
  depends_on  = [azurerm_api_management_named_value.access_token_issuer_url, null_resource.swa_deploy]
}

// /provisioning/{deviceId}/claim

resource "azurerm_api_management_api_operation" "device_claim" {
  operation_id        = "device-claim"
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  display_name        = "Claim device"
  method              = "POST"
  url_template        = "/{deviceId}/claim"

  template_parameter {
    name     = "deviceId"
    required = true
    type     = "string"
  }
}

resource "azurerm_api_management_api_operation_policy" "device_claim_policy" {
  api_name            = azurerm_api_management_api.provisioning_api.name
  api_management_name = azurerm_api_management.api_gateway.name
  resource_group_name = azurerm_resource_group.project_scope.name
  operation_id        = azurerm_api_management_api_operation.device_claim.operation_id

  xml_content = file("${path.module}/policies/ext_worker_claim.xml")

  depends_on = [
    azurerm_api_management_named_value.tenant_id,
    azurerm_api_management_named_value.external_worker_client_id,
    azurerm_api_management_named_value.function_app_client_id
  ]
}



// ===============================================
// Telemetry
// ===============================================

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

resource "azurerm_api_management_backend" "telemetry_backend" {
  name                = "telemetry-backend"
  resource_group_name = azurerm_resource_group.project_scope.name
  api_management_name = azurerm_api_management.api_gateway.name
  protocol            = "http"
  url                 = "https://${azurerm_function_app_flex_consumption.main_function.default_hostname}/api/v1"
}

// /device/telemetry

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

# resource "azurerm_api_management_diagnostic" "global_diagnostics" {
#   identifier               = "applicationinsights"
#   resource_group_name      = azurerm_resource_group.project_scope.name
#   api_management_name      = azurerm_api_management.api_gateway.name
#   api_management_logger_id = azurerm_api_management_logger.apim_logger.id

#   verbosity                 = var.environment == "prod" ? "information" : "verbose"
#   http_correlation_protocol = "W3C"
#   always_log_errors         = true
#   log_client_ip             = true
#   sampling_percentage       = var.environment == "prod" ? 5.0 : 100.0

#   frontend_request {
#     body_bytes     = 0
#     headers_to_log = ["User-Agent"]
#   }

#   backend_request {
#     body_bytes     = 0
#     headers_to_log = ["traceparent"]
#   }

#   backend_response {
#     body_bytes     = 0
#     headers_to_log = []
#   }
# }
