using FluentValidation;

namespace EventHub.Application.Features.Events.GetMyEvents
{
    public class GetMyEventsQueryValidator : AbstractValidator<GetMyEventsQuery>
    {
        public GetMyEventsQueryValidator()
        {
            RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(query => query.PageSize).InclusiveBetween(1, 50);

            string[] allowedSortColumns = ["Title", "Location", "EventDate", "Status"];
            RuleFor(query => query.SortColumn)
                .Must(column => string.IsNullOrEmpty(column) || allowedSortColumns.Contains(column))
                .WithMessage($"Sort column must be one of: {string.Join(", ", allowedSortColumns)}");
            RuleFor(query => query.SortDirection)
                .Must(direction => string.IsNullOrEmpty(direction) ||
                    direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                    direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Sort direction must be 'asc' or 'desc'.");
        }
    }
}
