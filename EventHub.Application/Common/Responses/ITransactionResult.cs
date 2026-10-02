namespace EventHub.Application.Common.Responses;

/// <summary>
/// Marks an application result whose failure must prevent the surrounding unit of work from committing.
/// </summary>
public interface ITransactionResult
{
    bool IsSuccess { get; }
}
