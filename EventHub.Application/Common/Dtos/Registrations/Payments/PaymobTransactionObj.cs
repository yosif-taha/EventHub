
namespace EventHub.Application.Common.Dtos.Registrations.Payments
{
    public class PaymobTransactionObj
    {
        public long Id { get; set; } 
        public bool Success { get; set; } 

        public PaymobOrderDetails Order { get; set; } = null!;
    }
}
