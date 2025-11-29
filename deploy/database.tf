resource "azurerm_cosmosdb_account" "iot_database_account" {
  name                = "cosmos-${var.project_name}-${var.environment}-${local.sa_org_suffix}"
  location            = azurerm_resource_group.project_scope.location
  resource_group_name = azurerm_resource_group.project_scope.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"
  tags                = var.tags

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.project_scope.location
    failover_priority = 0
  }

  capabilities {
    name = "EnableServerless"
  }
}

resource "azurerm_cosmosdb_sql_database" "telemetry_db" {
  name                = "${var.project_name}-db"
  resource_group_name = azurerm_resource_group.project_scope.name
  account_name        = azurerm_cosmosdb_account.iot_database_account.name
}

resource "azurerm_cosmosdb_sql_container" "telemetry_container" {
  name                = "device-registry"
  resource_group_name = azurerm_resource_group.project_scope.name
  account_name        = azurerm_cosmosdb_account.iot_database_account.name
  database_name       = azurerm_cosmosdb_sql_database.telemetry_db.name
  partition_key_paths = ["/deviceId"]
}

resource "azurerm_cosmosdb_sql_container" "telemetry_views" {
  name                = "views"
  resource_group_name = azurerm_resource_group.project_scope.name
  account_name        = azurerm_cosmosdb_account.iot_database_account.name
  database_name       = azurerm_cosmosdb_sql_database.telemetry_db.name
  partition_key_paths = ["/deviceId"]
}

resource "azurerm_cosmosdb_sql_container" "telemetry_raw" {
  name                = "telemetry-raw"
  resource_group_name = azurerm_resource_group.project_scope.name
  account_name        = azurerm_cosmosdb_account.iot_database_account.name
  database_name       = azurerm_cosmosdb_sql_database.telemetry_db.name
  partition_key_paths = ["/deviceId"]
}