
namespace Frontend.Model
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
