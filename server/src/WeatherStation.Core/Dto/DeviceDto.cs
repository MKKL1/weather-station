namespace WeatherStation.Core.Dto;

public record struct DeviceDto(string Id, Guid? OwnerId);