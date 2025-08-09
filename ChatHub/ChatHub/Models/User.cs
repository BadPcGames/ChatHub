namespace ChatHub.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = "";
        public string ConnectionId { get; set; } = "";
        public DateTime ConnectedAt { get; set; }
    }
}
