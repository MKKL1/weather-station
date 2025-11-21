locals {
  # Sanitize inputs to ensure they only contain alphanumeric characters for Storage Account requirements.
  sa_project_name = lower(replace(var.project_name, "/[^a-zA-Z0-9]/", ""))
  sa_env_name     = lower(replace(var.environment, "/[^a-zA-Z0-9]/", ""))
  sa_org_suffix   = lower(replace(var.org_suffix, "/[^a-zA-Z0-9]/", ""))

  # Globally unique name
  storage_account_name = var.storage_account_name != "" ? var.storage_account_name : "st${substr(local.sa_project_name, 0, 10)}${substr(local.sa_env_name, 0, 3)}${local.sa_org_suffix}"
}