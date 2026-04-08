using AutoMapper;
using EventHub.Application.Common.Dtos.Category;
using EventHub.Application.Features.Category.Create_Category;
using EventHub.Domin.Models;

namespace EventHub.Application.Common.Mapping.Category
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<CreateCategoryCommand, EventCategory>();
            CreateMap<EventCategory, CategoryDto>()
                .ForMember(dest => dest.EventsCount, opt => opt.MapFrom(src => src.Events));
        }
    }
}
