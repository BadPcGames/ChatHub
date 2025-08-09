using ChatHub.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChatHub.Logic
{
    public class ChatHub : Hub
    {
        private readonly ChatMessageStore _store;
        private static int _connections = 0;

        public ChatHub(ChatMessageStore store)
        {
            _store = store;
        }

        public override async Task OnConnectedAsync()
        {
            var name = Context.GetHttpContext()?.Request.Query["username"].ToString() ?? "Anonymous";

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = name,
                ConnectionId = Context.ConnectionId,
                ConnectedAt = DateTime.UtcNow
            };

            _store.AddUser(user);

            await Clients.Caller.SendAsync("SetCurrentUser", user);

            var count = System.Threading.Interlocked.Increment(ref _connections);
            await Clients.All.SendAsync("UserCount", count);

            var joinMsg = new ChatMessage
            {
                UserId = null,
                Text = $"{name} joined",
                Timestamp = DateTime.UtcNow
            };
            _store.AddMessage(joinMsg);
            await Clients.All.SendAsync("ReceiveMessage", joinMsg);

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(Guid userId, string text)
        {
            var user = _store.GetAllUsers().FirstOrDefault(u => u.Id == userId);
            if (user == null) return;

            var msg = new ChatMessage
            {
                UserId = user.Id,
                Text = text,
                Timestamp = DateTime.UtcNow
            };
            _store.AddMessage(msg);
            await Clients.All.SendAsync("ReceiveMessage", msg);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var name = Context.GetHttpContext()?.Request.Query["username"].ToString() ?? "Anonymous";

            var count = System.Threading.Interlocked.Decrement(ref _connections);
            await Clients.All.SendAsync("UserCount", count);

            var leaveMsg = new ChatMessage
            {
                Text = $"{name} left",
                Timestamp = DateTime.UtcNow
            };
            _store.AddMessage(leaveMsg);
            await Clients.All.SendAsync("ReceiveMessage", leaveMsg);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<IEnumerable<ChatMessage>> GetHistoryAsync()
        {
            return await Task.FromResult(_store.GetAllMessages());
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await Task.FromResult(_store.GetAllUsers());
        }
    }
}

