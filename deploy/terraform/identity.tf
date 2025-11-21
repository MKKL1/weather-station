resource "random_uuid" "provisioning_role_id" {}

resource "azuread_application" "function_app_auth" {
  display_name     = "fn-${var.project_name}-${var.environment}-provisioning-auth"
  sign_in_audience = "AzureADMyOrg"

  api {
    requested_access_token_version = 2
  }

  app_role {
    allowed_member_types = ["Application"]
    description          = "Allow APIM to call the Function App"
    display_name         = "APIM.Access"
    enabled              = true
    id                   = random_uuid.provisioning_role_id.result
    value                = "APIM.Access"
  }
}

resource "azuread_application_identifier_uri" "function_app_auth" {
  application_id = azuread_application.function_app_auth.id
  identifier_uri = "api://${azuread_application.function_app_auth.client_id}"
}

resource "azuread_service_principal" "function_app_auth" {
  client_id                    = azuread_application.function_app_auth.client_id
  app_role_assignment_required = true
  depends_on                   = [azuread_application_identifier_uri.function_app_auth]
}

resource "random_uuid" "main_role_id" {}

resource "azuread_application" "function_app_main_auth" {
  display_name     = "fn-${var.project_name}-${var.environment}-main-auth"
  sign_in_audience = "AzureADMyOrg"

  api {
    requested_access_token_version = 2
  }

  app_role {
    allowed_member_types = ["Application"]
    description          = "Allow APIM to call the Main Function App"
    display_name         = "APIM.Access"
    enabled              = true
    id                   = random_uuid.main_role_id.result
    value                = "APIM.Access"
  }
}

resource "azuread_application_identifier_uri" "function_app_main_auth" {
  application_id = azuread_application.function_app_main_auth.id
  identifier_uri = "api://${azuread_application.function_app_main_auth.client_id}"
}

resource "azuread_service_principal" "function_app_main_auth" {
  client_id                    = azuread_application.function_app_main_auth.client_id
  app_role_assignment_required = true
  depends_on                   = [azuread_application_identifier_uri.function_app_main_auth]
}

resource "azuread_app_role_assignment" "apim_to_function_app" {
  app_role_id         = random_uuid.provisioning_role_id.result
  principal_object_id = azurerm_api_management.api_gateway.identity[0].principal_id
  resource_object_id  = azuread_service_principal.function_app_auth.object_id
}

resource "azuread_app_role_assignment" "apim_to_main_function_app" {
  app_role_id         = random_uuid.main_role_id.result
  principal_object_id = azurerm_api_management.api_gateway.identity[0].principal_id
  resource_object_id  = azuread_service_principal.function_app_main_auth.object_id
}