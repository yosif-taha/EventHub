
namespace EventHub.Domin.Common
{
    public class BaseModel
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public DateTime? CreatedAt { get; set; } 
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
