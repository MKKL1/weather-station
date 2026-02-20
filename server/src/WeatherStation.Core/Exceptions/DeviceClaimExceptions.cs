namespace WeatherStation.Core.Exceptions;

public class DeviceAlreadyClaimedException(string deviceId)
    : DomainException($"Device {deviceId} is already claimed by another user.", "DEVICE_ALREADY_CLAIMED");

public class InvalidAuthCodeException()
    : DomainException("The provided authentication code is invalid.", "INVALID_AUTH_CODE");

public class InvalidClaimWordsException(string deviceId)
    : DomainException($"Provided device claim words code do not match {deviceId}", "INVALID_CLAIM_WORDS");