using JetBrains.Annotations;
using WeatherStation.Core.Services;
using Xunit;

namespace WeatherStation.Core.Tests.Services;

[TestSubject(typeof(DeviceAuthenticationService))]
public class DeviceAuthenticationServiceTest
{
    private readonly DeviceAuthenticationService _service = new();
    [Fact]
    public void GivenMatchingDeviceIdAndWords_ReturnTrue()
    {
        Assert.True(_service.VerfiyDeviceIdAgainstWords(
            "H1-AGAQEZML3D772QT4UKS43NJQ",
            "account around kingdom deal engine pudding parade another calm juice pig burst"));
    }
}