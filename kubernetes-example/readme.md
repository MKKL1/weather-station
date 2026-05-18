This is an example of how this service could be deployed thru kubernetes.
When deploying to azure you may consider using real CosmosDB instead of emulator.

All keys presented here are examples, not used in production. To generate new keys execute those commands:
```sh
openssl genrsa -out ./keys/private.pem 2048
openssl rsa -in ./keys/private.pem -pubout -out ./keys/public.pem
openssl genrsa -out ./keys/provisioning-private.pem 2048
openssl rsa -in ./keys/provisioning-private.pem -pubout -out ./keys/provisioning-public.pem

kubectl create secret generic ws-keys -n weather-app-dev --from-file=private.pem=./keys/private.pem --from-file=public.pem=./keys/public.pem --from-file=provisioning-private.pem=./keys/provisioning-private.pem --from-file=provisioning-public.pem=./keys/provisioning-public.pem --dry-run=client -o yaml > ws-keys.yaml
```

To regenerate secrets.yaml when `.env` changes use:
```sh
kubectl create secret generic ws-secrets --from-env-file=.env -n weather-app-dev --dry-run=client -o yaml > secrets.yaml
```
