resource "azurerm_static_web_app" "public_keys_host" {
  name                = "swa-${var.project_name}-${var.environment}-jwks"
  resource_group_name = azurerm_resource_group.project_scope.name
  location            = var.location
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = var.tags
}

resource "azurerm_static_web_app_custom_domain" "jwks_domain" {
  count             = var.custom_jwks_domain != "" ? 1 : 0
  static_web_app_id = azurerm_static_web_app.public_keys_host.id
  domain_name       = var.custom_jwks_domain
  validation_type   = "cname-delegation"
}

resource "local_file" "device_openid_config" {
  content = jsonencode({
    issuer   = "https://${azurerm_static_web_app.public_keys_host.default_host_name}/device"
    jwks_uri = "https://${azurerm_static_web_app.public_keys_host.default_host_name}/device/.well-known/jwks.json"
  })
  filename = "${var.jwks_source_folder}/device/.well-known/openid-configuration"
}

resource "local_file" "provisioning_openid_config" {
  content = jsonencode({
    issuer   = "https://${azurerm_static_web_app.public_keys_host.default_host_name}/provisioning"
    jwks_uri = "https://${azurerm_static_web_app.public_keys_host.default_host_name}/provisioning/.well-known/jwks.json"
  })
  filename = "${var.jwks_source_folder}/provisioning/.well-known/openid-configuration"
}

resource "null_resource" "swa_deploy" {
  triggers = {
    swa_id      = azurerm_static_web_app.public_keys_host.id
    device_conf = local_file.device_openid_config.content
    prov_conf   = local_file.provisioning_openid_config.content
  }

  provisioner "local-exec" {
    command = "swa deploy ${var.jwks_source_folder} --env production --deployment-token ${azurerm_static_web_app.public_keys_host.api_key} --no-use-keychain"
  }

  depends_on = [
    local_file.device_openid_config,
    local_file.provisioning_openid_config
  ]
}