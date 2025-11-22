# terraform {
#   required_providers {
#     azurerm = {
#       source  = "hashicorp/azurerm"
#       version = "~> 4.0" 
#     }
#     azuread = {
#       source  = "hashicorp/azuread"
#       version = "~> 2.0"
#     }
#     random = {
#       source  = "hashicorp/random"
#       version = "~> 3.0"
#     }
#   }
# }

# provider "azurerm" {
#   features {
#     key_vault {
#       purge_soft_delete_on_destroy = true
#     }
#     resource_group {
#       prevent_deletion_if_contains_resources = false
#     }
#   }
# }