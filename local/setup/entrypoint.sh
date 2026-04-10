#!/usr/bin/env bash
set -euo pipefail

OUTPUT_DIR="${OUTPUT_DIR:-/output}"
KEYS_DIR="${KEYS_DIR:-$OUTPUT_DIR/keys}"
DEVICES_FILE="${DEVICES_FILE:-$OUTPUT_DIR/devices.json}"
DEVICE_COUNT="${DEVICE_COUNT:-3}"

DEVICE_PRIVATE_KEY="$KEYS_DIR/private.pem"
DEVICE_PUBLIC_KEY="$KEYS_DIR/public.pem"
PROVISIONING_PRIVATE_KEY="$KEYS_DIR/provisioning-private.pem"
PROVISIONING_PUBLIC_KEY="$KEYS_DIR/provisioning-public.pem"

mkdir -p "$KEYS_DIR"
mkdir -p "$(dirname "$DEVICES_FILE")"

umask 077

device_keys_changed=0
provisioning_keys_changed=0

generate_keypair() {
    local private_key_path="$1"
    local public_key_path="$2"
    local label="$3"
    local changed_var_name="$4"

    if [ -f "$private_key_path" ]; then
        if [ ! -f "$public_key_path" ]; then
            echo "Public key for '$label' missing, deriving it from private key..."
            openssl rsa -in "$private_key_path" -pubout -out "$public_key_path" >/dev/null 2>&1
            chmod 644 "$public_key_path"
        else
            echo "Key pair for '$label' already exists, skipping generation."
        fi
        return 0
    fi

    if [ -f "$public_key_path" ]; then
        echo "Private key for '$label' is missing; regenerating full key pair to keep keys matched..."
        rm -f "$public_key_path"
    else
        echo "Generating key pair for '$label'..."
    fi

    openssl genrsa -out "$private_key_path" 2048 >/dev/null 2>&1
    chmod 644 "$private_key_path"

    openssl rsa -in "$private_key_path" -pubout -out "$public_key_path" >/dev/null 2>&1
    chmod 644 "$public_key_path"

    printf -v "$changed_var_name" '1'
}

generate_keypair "$DEVICE_PRIVATE_KEY" "$DEVICE_PUBLIC_KEY" "device" device_keys_changed
generate_keypair "$PROVISIONING_PRIVATE_KEY" "$PROVISIONING_PUBLIC_KEY" "provisioning" provisioning_keys_changed

if [ ! -f "$DEVICES_FILE" ] || [ "$provisioning_keys_changed" = "1" ]; then
    echo "Generating ${DEVICE_COUNT} device credentials..."
    /opt/venv/bin/python3 /scripts/generate_devices.py \
        --key "$PROVISIONING_PRIVATE_KEY" \
        --out "$DEVICES_FILE" \
        --count "$DEVICE_COUNT"
else
    echo "Device file already exists and provisioning key was unchanged, skipping device generation."
fi

echo "Setup completed."