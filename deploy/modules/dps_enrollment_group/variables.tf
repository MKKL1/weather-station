variable "subscription_id" {
  description = "Subscription to use to create all resources. Defaults to default subscription."
  default     = "{subscriptionId}"
}

variable "resource-group" {
  description = "The Resource Group where all resources will be added"
}

variable "iothub" {
  description = "The IoTHub where add the enrollment group"
}

variable "iothub-hostname" {
  description = "The IoTHub where add the enrollment group"
}

variable "dps" {
  description = "The DPS where add the enrollment group"
}

variable "enrollment-name" {
  description = "The enrollment group name"
}

variable "initial-twin-tags" {
  default = {}
  description = "The Initial Twin for the DPS EnrollmentGroup associated to the IoTHub."
}