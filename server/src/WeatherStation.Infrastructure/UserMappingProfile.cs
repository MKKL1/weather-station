using AutoMapper;
using WeatherStation.Domain.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, Users>()
            .ForMember(dest => dest.Devices, opt => opt.Ignore());
            
        CreateMap<Users, User>();
    }
}