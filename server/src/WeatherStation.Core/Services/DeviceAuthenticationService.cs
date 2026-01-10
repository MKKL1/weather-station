using Bitcoin.BIP39;
using SimpleBase;

namespace WeatherStation.Core.Services;

public class DeviceAuthenticationService
{
    //Handles 12 words device authentication.
    //Verifies if bip39 words form given device id (initial proof of possession)
    public bool VerfiyDeviceIdAgainstWords(string deviceId, string words)
    {
        var seed = BIP39.GetSeedBytes(words);
        var rawHash = seed[..12];
        var suffixStr = Base32.Rfc4648.Encode(rawHash).TrimEnd('=');

        return deviceId[^20..] == suffixStr;
    }
}