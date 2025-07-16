using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherStation.Infrastructure.Tables;

public class Users
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ICollection<Devices> Devices { get; set; } = new List<Devices>();
}
