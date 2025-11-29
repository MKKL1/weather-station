resource "azurerm_storage_account" "backend_storage" {
  name                     = local.storage_account_name
  resource_group_name      = azurerm_resource_group.project_scope.name
  location                 = azurerm_resource_group.project_scope.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags                     = var.tags
}

resource "azurerm_storage_container" "deployment_packages_main" {
  name                  = var.function_app_container_name
  storage_account_id    = azurerm_storage_account.backend_storage.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "deployment_packages_provisioning" {
  name                  = "${var.function_app_container_name}-provisioning"
  storage_account_id    = azurerm_storage_account.backend_storage.id
  container_access_type = "private"
}