using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherStation.Infrastructure.Tables;

//This is used by services in azure, therefore this shouldn't be the source of information
public class DeviceDb
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; set; } //PK
    public Guid UserId { get; set; } //FK
    public UserDb UserDb { get; set; }
}
