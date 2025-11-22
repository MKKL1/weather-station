data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "secrets_vault" {
  name                        = "kv-${var.project_name}-${var.environment}"
  location                    = azurerm_resource_group.project_scope.location
  resource_group_name         = azurerm_resource_group.project_scope.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false
  sku_name                    = "standard"

  tags = var.tags
}

resource "azurerm_key_vault_access_policy" "terraform_user_access" {
  key_vault_id = azurerm_key_vault.secrets_vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = ["Get", "List", "Set", "Delete", "Purge", "Recover"]
}

resource "azurerm_key_vault_secret" "token_signing_key" {
  name         = "access-token-private-key"
  value        = var.access_token_private_key
  key_vault_id = azurerm_key_vault.secrets_vault.id
  tags         = var.tags

  # Ensure the policy exists before trying to write the secret
  depends_on = [azurerm_key_vault_access_policy.terraform_user_access]
}