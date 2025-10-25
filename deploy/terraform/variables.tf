variable "subscription_id" {
  description = "Subscription ID for Azure resources"
  type        = string
}

variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "weather-station"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "West Europe"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "dev"
}

variable "storage_account_name" {
  description = "The name for the storage account. Must be globally unique, 3-24 chars, lowercase alphanumeric. If left empty, a unique name will be generated."
  type        = string
  default     = ""
}

variable "function_app_container_name" {
  description = "Name for the storage container for the function app. Must be lowercase, 3-63 chars, alphanumeric, and hyphens."
  type        = string
  default     = "function-app-files"
}
