resource "azurerm_log_analytics_workspace" "central_logs" {
  name                = "log-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.project_scope.location
  resource_group_name = azurerm_resource_group.project_scope.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_application_insights" "backend_tracing" {
  name                = "appi-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.project_scope.location
  resource_group_name = azurerm_resource_group.project_scope.name
  workspace_id        = azurerm_log_analytics_workspace.central_logs.id
  application_type    = "web"
  tags                = var.tags
}