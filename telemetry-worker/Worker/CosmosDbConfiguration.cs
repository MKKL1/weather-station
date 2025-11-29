namespace Worker;

public record CosmosDbConfiguration(
    string ConnectionString,
    string DatabaseName,
    string ViewsContainerName,
    string TelemetryContainerName);