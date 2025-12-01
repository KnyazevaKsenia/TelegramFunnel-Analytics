using AutoMapper;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;
using Telegram_Analytic.Models.ProjectModels;

namespace Telegram_Analytic.Infrastructure.AutoMapperProfiles;


public class TrackingLinkProfile : Profile
{
    public TrackingLinkProfile()
    {
        CreateMap<TrackingLink, TrackingLinkViewModel>();
        
    }
}

public class ProjectProfile : Profile
{
    public ProjectProfile()
    {
        CreateMap<Project, ProjectViewModel>()
            .ForMember(dest => dest.TrackingLinks, opt => opt.Ignore());
    }
}
