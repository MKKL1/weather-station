namespace WeatherStation.Core.Exceptions;

public class DeviceAlreadyClaimedBySelfException : DomainException
{
    public DeviceAlreadyClaimedBySelfException(string deviceId) 
        : base($"You have already claimed the {deviceId} device", "DEVICE_CLAIMED_CONFLICT")
    {
    }
}

public class DeviceAlreadyClaimedByOtherException : DomainException
{
    public DeviceAlreadyClaimedByOtherException(string deviceId) 
        : base($"Device {deviceId} is already claimed by another user.", "DEVICE_ALREADY_CLAIMED")
    {
    }
}

public class InvalidAuthCodeException : DomainException
{
    public InvalidAuthCodeException() 
        : base("The provided authentication code is invalid.", "INVALID_AUTH_CODE")
    {
    }
}

public class InvalidClaimWordsException : DomainException
{
    public InvalidClaimWordsException(string deviceId) 
        : base($"Provided device claim words code do not match {deviceId}", "INVALID_CLAIM_WORDS")
    {
    }
}