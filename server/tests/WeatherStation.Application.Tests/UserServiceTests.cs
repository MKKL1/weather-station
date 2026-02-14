using Moq;
using WeatherStation.Core.Services;
using WeatherStation.Domain.Entities;
using WeatherStation.Domain.Repositories;

namespace WeatherStation.Application.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly UserService _userService;
    
    public UserServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _userService = new UserService(_userRepository.Object);
    }

    [Fact]
    public async Task CreateUserIfNotExists_ReturnsExistingUser_IfEmailMatches()
    {
        var email = "test@gmail.com";
        var user = new User
        {
            Id = Guid.NewGuid(), //Not sure if I can generate random uuid
            Name = "testname",
            Email = email
        };
        
        _userRepository.Setup(r => r.GetUserByEmail(email, CancellationToken.None))
            .ReturnsAsync(user);

        var returnedUser = await _userService.GetOrCreateUser(email, "test2", CancellationToken.None);
        //TODO probably not the best way to compare objects
        Assert.Equal(user.Id, returnedUser.Id);
        Assert.Equal(user.Email, user.Email);
        Assert.Equal(user.Name, returnedUser.Name);
    }
    
    [Fact]
    public async Task CreateUserIfNotExists_CallsCreateUser_IfEmailNotMatches()
    {
        var email = "test@gmail.com";
        var name = "test2";
        
        _userRepository.Setup(r => r.GetUserByEmail(email, CancellationToken.None))
            .ReturnsAsync((User?)null);

        var returnedUser = await _userService.GetOrCreateUser(email, name, CancellationToken.None);
        _userRepository.Verify(r => r.CreateUser(
                It.Is<User>(u => u.Email == email && u.Name == name), 
                CancellationToken.None
            ),
            Times.Once);
        
        //TODO probably not the best way to compare objects
        Assert.Equal(email, returnedUser.Email);
        Assert.Equal(name,  returnedUser.Name);
    }
}