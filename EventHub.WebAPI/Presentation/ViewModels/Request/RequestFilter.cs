namespace EventHub.WebAPI.Presentation.ViewModels.Request
{
    public record RequestFilter
    {
        // Default values for pagination parameters
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        // Optional search value for filtering results
        public string? SearchValue { get; init; }

        // Optional sorting parameters
        public string? SortColumn { get; init; }
        public string SortDirection { get; init; } = "asc";
    }
}
