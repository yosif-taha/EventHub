namespace EventHub.MVC.Models.Events;

public sealed class EventListFilter
{
    public string? SearchValue { get; set; }
    public Guid? CategoryId { get; set; }
    public string? SortColumn { get; set; }
    public string SortDirection { get; set; } = "asc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
