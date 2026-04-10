import os
import sys
import urllib3
from azure.cosmos import CosmosClient, PartitionKey
from azure.cosmos.exceptions import CosmosHttpResponseError

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

endpoint = os.environ.get("COSMOS_ENDPOINT", "https://cosmos-emulator:8081/")
key = os.environ.get("COSMOS_KEY", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
database_name = os.environ.get("COSMOS_DATABASE", "weather-station-db")

print(f"Connecting to Cosmos DB {endpoint}")
client = CosmosClient(endpoint, credential=key, connection_verify=False, enable_endpoint_discovery=False)

try:
    print(f"Ensuring database '{database_name}' exists...")
    database = client.create_database_if_not_exists(id=database_name)

    containers = [
        {"id": "device-registry", "pk": "/deviceId"},
        {"id": "views", "pk": "/deviceId"},
        {"id": "telemetry-raw", "pk": "/deviceId"}
    ]

    for c in containers:
        print(f"Ensuring container '{c['id']}' exists...")
        database.create_container_if_not_exists(
            id=c['id'],
            partition_key=PartitionKey(path=c['pk'])
        )

    print("Cosmos DB successfully configured!")
except CosmosHttpResponseError as e:
    print(f"Initialization failed: {e}")
    sys.exit(1)
