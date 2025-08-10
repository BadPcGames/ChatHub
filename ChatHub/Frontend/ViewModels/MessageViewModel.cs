namespace FrontEnd.ViewModels
{
    public class MessageViewModel
    {
        public string Username { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string TimestampLocal => Timestamp.ToLocalTime().ToString("HH:mm");
        public bool ItIsYou { get; set; }
        public string AvatarImage { get; set; }
    }
}
