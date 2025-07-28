#https://github.com/hashicorp/terraform-provider-azurerm/issues/11101#issuecomment-1477876486

# Create DPS Enrollment Group, which is not yet supported by Terraform
# Note: Keys (primary and secondary) are created automatically if not passed
# Note: Needs extra plugin `azure-iot` installed
# Note: azure-iot extension for Azure CLI requires AZ version 2.17.1 or higher
# - https://www.terraform.io/docs/language/resources/provisioners/local-exec.html
# - https://www.terraform.io/docs/language/expressions/strings.html#indented-heredocs
# - https://registry.terraform.io/providers/hashicorp/null/latest/docs/resources/resource
# - https://docs.microsoft.com/en-us/cli/azure/iot/dps/enrollment-group?view=azure-cli-latest
# - https://github.com/hashicorp/terraform-provider-azurerm/issues/11101

locals {
  # Set here enrollment options
  # Note: If no certificate_path and not root_ca_name (nor secondaries), attestation type will be "Symmetric Key"
  # Note: Keys (primary and secondary) are created automatically if not passed
  enrollment_envs = {
    SUBSCRIPTION       = var.subscription_id
    RG                 = var.resource-group
    DPS                = var.dps
    ALLOC_POLICY       = "hashed"
    EDGE_ENABLED       = "true"
    IOT_HOSTNAME       = var.iothub-hostname
    PROVISION_STATUS   = "enabled"
    REPROVISION_POLICY = "reprovisionandmigratedata"

    NAME = var.enrollment-name
    INITIAL_TWIN = jsonencode({
      tags = var.initial-twin-tags
    })
  }
}

resource "null_resource" "enrollment-group" {
  # Re-launch this resource whenever one or more of these change
  triggers = local.enrollment_envs

  # Needs this resources to be created before running
  depends_on = [
    var.resource-group,
    var.dps
  ]

  # Remove resource on triggers change
  # TODO: Translate to Python SDK
  provisioner "local-exec" {
    environment = self.triggers
    when        = destroy
    command     = "${path.module}/remove.sh"
  }

    # Create resource
    # TODO: Translate to Python SDK
    provisioner "local-exec" {
        environment = self.triggers
        command     = "${path.module}/enroll.sh"
    }
}