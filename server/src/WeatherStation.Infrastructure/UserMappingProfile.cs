using AutoMapper;
using WeatherStation.Domain.Entities;
using WeatherStation.Infrastructure.Tables;

namespace WeatherStation.Infrastructure;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDao>()
            .ForMember(dest => dest.Devices, opt => opt.Ignore());
            
        CreateMap<UserDao, User>();
    }
}