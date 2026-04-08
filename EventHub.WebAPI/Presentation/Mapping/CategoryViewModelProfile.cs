using AutoMapper;
using EventHub.Application.Common.Dtos.Category;
using EventHub.WebAPI.Presentation.ViewModels.Category;

namespace EventHub.WebAPI.Presentation.Mapping
{
    public class CategoryViewModelProfile : Profile
    {
        public CategoryViewModelProfile()
        {
            CreateMap<CategoryDto,GetAllCategoriesViewModel>();
            CreateMap<CategoryDto,GetCategoryByIdViewModel>();
        }
    }
}
