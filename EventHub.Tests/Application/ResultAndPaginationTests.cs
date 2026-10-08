using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using Xunit;

namespace EventHub.Tests.Application;

public sealed class ResultAndPaginationTests
{
    [Fact]
    public void Results_PreserveSuccessDataAndFailureDetails()
    {
        // Arrange / Act
        var success = RequestResult<int>.Success(42);
        var failure = RequestResult<int>.Failure(ErrorCode.Forbidden, "denied");

        // Assert
        Assert.True(((ITransactionResult)success).IsSuccess);
        Assert.Equal(42, success.Data);
        Assert.Equal(ErrorCode.None, success.ErrorCode);
        Assert.False(failure.IsSuccess);
        Assert.Equal(ErrorCode.Forbidden, failure.ErrorCode);
        Assert.Equal("denied", failure.Message);
        Assert.Equal(0, failure.Data);
    }

    [Theory]
    [InlineData(0, 1, 10, 0, false, false)]
    [InlineData(11, 1, 10, 2, false, true)]
    [InlineData(11, 2, 10, 2, true, false)]
    [InlineData(10, 1, 10, 1, false, false)]
    [InlineData(10, 1, 0, 0, false, false)]
    public void Pagination_ComputesPageCountAndNavigation(int count, int page, int size, int pages, bool previous, bool next)
    {
        // Arrange / Act
        var result = new PaginatedList<int>([1], count, page, size);

        // Assert
        Assert.Equal(pages, result.TotalPages);
        Assert.Equal(previous, result.HasPreviousPage);
        Assert.Equal(next, result.HasNextPage);
        Assert.Equal(count, result.TotalCount);
        Assert.Equal([1], result.Items);
    }
}
