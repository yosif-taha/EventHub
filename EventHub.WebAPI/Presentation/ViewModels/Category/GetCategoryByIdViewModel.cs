namespace EventHub.WebAPI.Presentation.ViewModels.Category
{
    public record GetCategoryByIdViewModel(
      Guid Id,
      string Name,
      int EventsCount
    );
}
