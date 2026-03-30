public class ProductAlert
{
    public int Id { get; set; }
    public int UserId { get; set; }              // FK to User.Id
    public int ProductId { get; set; }           // FK to Product.Id
    public int FrequencyDays { get; set; }       // 1,2,3,5,7
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastSentAt { get; set; }    // nullable until first send
    public DateTime NextSendAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
