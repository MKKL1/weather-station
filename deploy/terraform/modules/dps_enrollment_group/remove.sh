#!/usr/bin/env bash
set -e

echo "Enrollment group changed. Removing."
az iot dps enrollment-group delete \
--subscription "$SUBSCRIPTION" \
--resource-group "$RG" \
--dps-name "$DPS" \
--enrollment-id "$NAME"