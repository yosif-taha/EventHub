namespace EventHub.WebAPI.Presentation.ViewModels.Category
{
    public record GetAllCategoriesViewModel(
      Guid Id,
      string Name,
      int EventsCount
    );
}
