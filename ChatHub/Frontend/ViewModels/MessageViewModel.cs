namespace FrontEnd.ViewModels
{
    public class MessageViewModel
    {
        public string Username { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string TimestampLocal => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        public bool ItIsYou { get; set; }
    }
}
