#!/usr/bin/env bash
set -e

# First, test if resource already exists
ACTION="create"
if az iot dps enrollment-group list \
     --subscription "$SUBSCRIPTION" \
     --resource-group "$RG" \
     --dps-name "$DPS" \
   | grep "\"$NAME\"" > /dev/null
then
    ACTION="update"
    echo "Enrollment group already exists. Updating."
fi

# Create or update the enrollment group
az iot dps enrollment-group "$ACTION" \
  --subscription "$SUBSCRIPTION" \
  --resource-group "$RG" \
  --dps-name "$DPS" \
  --enrollment-id "$NAME" \
  --edge-enabled "$EDGE_ENABLED" \
  --iot-hubs "$IOT_HOSTNAME" \
  --initial-twin-properties "$INITIAL_TWIN" \
  --provisioning-status "$PROVISION_STATUS" \
  --reprovision-policy "$REPROVISION_POLICY" \
  --allocation-policy "$ALLOC_POLICY"
