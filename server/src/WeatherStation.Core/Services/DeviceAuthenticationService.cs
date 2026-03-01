using Bitcoin.BIP39;
using SimpleBase;

namespace WeatherStation.Core.Services;

public static class DeviceAuthenticationService
{
    // Verifies if BIP39 words form the given device ID (initial proof of possession).
    // Handles 12-word device authentication by checking the last 20 characters of the device ID.
    public static bool VerifyDeviceIdAgainstWords(string deviceId, string words)
    {
        var seed = BIP39.GetSeedBytes(words);
        var rawHash = seed[..12];
        var suffixStr = Base32.Rfc4648.Encode(rawHash).TrimEnd('=');

        return deviceId[^20..] == suffixStr;
    }
}