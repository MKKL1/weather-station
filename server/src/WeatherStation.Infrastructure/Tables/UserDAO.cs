﻿using System.ComponentModel.DataAnnotations;

namespace WeatherStation.Infrastructure.Tables;

public class UserDao
{
    [Key]
    public Guid Id { get; set; } //We will user id unique to our application as different identity providers may do it differently
    //Better way to handling this problem is by distinguishing users by theirs Emails which requires idp to verify emails
    //We may have to provide a setting that allows to identify by different email if they are not unique (not top priority)
    public required string Email { get; set; }
    public required string Name { get; set; }
    public ICollection<DeviceDao> Devices { get; set; } = new List<DeviceDao>();
}
