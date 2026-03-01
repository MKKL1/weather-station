using JetBrains.Annotations;
using WeatherStation.Core.Services;
using Xunit;

namespace WeatherStation.Core.Tests.Services;

[TestSubject(typeof(DeviceAuthenticationService))]
public class DeviceAuthenticationServiceTest
{
    [Fact]
    public void GivenMatchingDeviceIdAndWords_ReturnTrue()
    {
        Assert.True(DeviceAuthenticationService.VerifyDeviceIdAgainstWords(
            "H1-AGAQEZML3D772QT4UKS43NJQ",
            "account around kingdom deal engine pudding parade another calm juice pig burst"));
    }
}