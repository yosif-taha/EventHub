using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Features.Events.GetAll_Events;
using EventHub.Application.Features.Events.GetManagedEventById;
using EventHub.Application.Features.Events.Check_Event_Availability;
using EventHub.Tests.Support;
using Moq;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EventHub.Tests.WebAPI;

[Trait("Category", "Http")]
public class EventReadHttpTests
{
    [Fact]
    public async Task PublicList_ForwardsFiltersPreservesTotalsAndStripsPrivateMeetingUrls()
    {
        // Arrange
        using var fixture = new ApiFixture();
        var command = new GetAllEventsQuery("Music", WorkflowFixture.OtherId, "Title", "desc", 2, 5);
        fixture.Mediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<PaginatedList<EventDto>>.Success(new([
                new() { Id = WorkflowFixture.OwnerId, Title = "Music conference", OnlineMeetingUrl = "https://private.example.test/" }
            ], 12, 2, 5)));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/events?searchValue=Music&categoryId=" + WorkflowFixture.OtherId + "&sortColumn=Title&sortDirection=desc&pageNumber=2&pageSize=5");

        // Assert
        var page = json.GetProperty("data");
        Assert.Equal(12, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page.GetProperty("totalPages").GetInt32());
        var row = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal("Music conference", row.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("onlineMeetingUrl").ValueKind);
        fixture.Mediator.VerifyAll();
    }

    [Fact]
    public async Task ManagementRead_PreservesMeetingUrlForAuthorizedOrganizer()
    {
        // Arrange
        using var fixture = new ApiFixture(); fixture.Authenticate("Organizer");
        fixture.Mediator.Setup(m => m.Send(new GetManagedEventByIdQuery(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<EventDto>.Success(new() { OnlineMeetingUrl = "https://private.example.test/" }));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/events/" + WorkflowFixture.OtherId + "/management");

        // Assert
        Assert.Equal("https://private.example.test/", json.GetProperty("data").GetProperty("onlineMeetingUrl").GetString());
        fixture.Mediator.VerifyAll();
    }

    [Fact]
    public async Task Availability_MapsAllCalculatedFields()
    {
        // Arrange
        using var fixture = new ApiFixture();
        fixture.Mediator.Setup(m => m.Send(new CheckEventAvailabilityQuery(WorkflowFixture.OtherId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RequestResult<EventAvailabilityDto>.Success(new(false, 3, true)));

        // Act
        var json = await fixture.Client.GetFromJsonAsync<JsonElement>("/api/Event/CheckEventAvailability?id=" + WorkflowFixture.OtherId);

        // Assert
        var data = json.GetProperty("data");
        Assert.False(data.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(3, data.GetProperty("remainingSlots").GetInt32());
        Assert.True(data.GetProperty("isCancelled").GetBoolean());
        fixture.Mediator.VerifyAll();
    }
}
