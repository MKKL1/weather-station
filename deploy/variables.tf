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