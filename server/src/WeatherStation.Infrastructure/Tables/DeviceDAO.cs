﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherStation.Infrastructure.Tables;

//TODO should probably rename it to DeviceEntity
public class DeviceDao
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; set; } //PK
    public Guid UserId { get; set; } //FK
    public UserDao UserDao { get; set; }
}
