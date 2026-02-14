using WeatherStation.Domain.ValueObjects;

namespace WeatherStation.Domain.Entities;

public class User
{
    public UserId Id { get; set; } = Guid.NewGuid();
    public Email Email { get; private set; } // Uses your new Value Object

    // Pure logic. No database calls here.
    public void ChangeEmail(Email newEmail) 
    {
        Email = newEmail;
    }
}