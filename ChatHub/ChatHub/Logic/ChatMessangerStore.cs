using ChatHub.Models;
using System.Collections.Concurrent;

namespace ChatHub.Logic
{
    public class ChatMessageStore
    {
        private readonly ConcurrentQueue<ChatMessage> _messages = new();
        private readonly ConcurrentQueue<User> _users = new();


        public void AddUser(User user)
        {
            _users.Enqueue(user);
            while (_users.Count > 1000 && _users.TryDequeue(out _)) { }
        }

        public IEnumerable<User> GetAllUsers() => _users.ToArray();

        public void AddMessage(ChatMessage message)
        {
            _messages.Enqueue(message);
            while (_messages.Count > 1000 && _messages.TryDequeue(out _)) { }
        }

        public IEnumerable<ChatMessage> GetAllMessages() => _messages.ToArray();
    }
}
