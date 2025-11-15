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

variable "apim_publisher_name" {
  description = "The publisher name for APIM."
  type        = string
  default     = "My Company"
}

variable "apim_publisher_email" {
  description = "The publisher email for APIM."
  type        = string
  default     = "admin@example.com"
}

variable "provisioning_public_key" {
  description = "Public key for validating provisioning JWTs"
  type        = string
  sensitive   = true
}

variable "access_token_private_key" {
  description = "Private key for signing access token JWTs (PEM format, base64 encoded)"
  type        = string
  sensitive   = true
}

variable "access_token_public_key" {
  description = "Public key for validating access token JWTs (PEM format, base64 encoded)"
  type        = string
  sensitive   = true
}