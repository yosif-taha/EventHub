using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Features.Category.Create_Category;
using EventHub.Application.Features.Category.Delete_Category;
using EventHub.Application.Features.Category.Get_All_Categories;
using EventHub.Application.Features.Category.Get_Category_By_Id;
using EventHub.Application.Features.Category.Update_Category;
using EventHub.WebAPI.Presentation.ViewModels.Category;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CategoryController(IMediator _mediator, IMapper _mapper) : ControllerBase
    {
        [HttpGet]
        public async Task<ResponseViewModel> GetAllCategories(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAllCategoriesQuery(),ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<List<GetAllCategoriesViewModel>>(result.Data);
            return new SuccessResponseViewModelT<List<GetAllCategoriesViewModel>>(data);
        }

        [HttpGet]
        public async Task<ResponseViewModel> GetCategoryById([FromQuery] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<GetCategoryByIdViewModel>(result.Data);
            return new SuccessResponseViewModelT<GetCategoryByIdViewModel>(data);
        }

        [HttpPost]
        public async Task<ResponseViewModel> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new CreateCategoryCommand(request.Name),ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModelT<Guid>(result.Data, "Category Created Successfuly");
        }

        [HttpPost]
        public async Task<ResponseViewModel> UpdateCategory([FromBody] UpdateCategoryRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new UpdateCategoryCommand(request.Id,request.Name), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModelT<Unit>(result.Data, "Category Updated Successfuly");
        }

        [HttpPost]
        public async Task<ResponseViewModel> DeleteCategory([FromQuery] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DeleteCategoryCommand(id), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModelT<Unit>(result.Data, "Category Deleted Successfuly");
        }
    }
}
