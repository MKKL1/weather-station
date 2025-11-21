variable "org_suffix" {
  description = "A short, unique suffix for your organization (e.g., 'contoso'). Critical for global uniqueness of Storage Accounts."
  type        = string
  default     = "myorg"
}

variable "project_name" {
  type    = string
  default = "weather-station"
}

variable "environment" {
  description = "Deployment stage (dev, staging, prod). Affects naming and resource tiers."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Primary Azure region for all resources."
  type        = string
  default     = "West Europe"
}

variable "access_token_private_key" {
  description = "The Private Key (PEM) used to sign JWTs."
  type        = string
  sensitive   = true
}

variable "access_token_public_key" {
  description = "The Public Key (PEM) used to validate JWTs."
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Standard tags for cost allocation and resource management."
  type        = map(string)
  default = {
    Project   = "weather-station"
    ManagedBy = "Terraform"
  }
}

variable "storage_account_name" {
  description = "Override for storage account name. If empty, one is generated automatically."
  type        = string
  default     = ""
}

variable "function_app_container_name" {
  type    = string
  default = "function-app-files"
}

variable "apim_publisher_name" {
  type    = string
  default = "My Company"
}

variable "apim_publisher_email" {
  type    = string
  default = "admin@example.com"
}

variable "custom_jwks_domain" {
  description = "Optional custom domain for the Static Web App serving JWKS keys."
  type        = string
  default     = ""
}

variable "jwks_source_folder" {
  description = "The local path to the folder containing JWKS, openid-configuration, and staticwebapp.config.json"
  type        = string
}