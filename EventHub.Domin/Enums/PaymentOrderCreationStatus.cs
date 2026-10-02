namespace EventHub.Domin.Enums;

/// <summary>
/// Tracks the durable lifecycle of creating a Paymob order for a local payment intent.
/// </summary>
public enum PaymentOrderCreationStatus
{
    Pending = 0,
    Processing = 1,
    Created = 2,
    Failed = 3
}
