using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FrontEnd.ViewModels;
using Frontend.Model;

namespace Frontend
{
    public partial class MainWindow : Window
    {
        private HubConnection? _connection;
        private Guid _currentUserId;
        private List<User> MessagesUsersCache = new();

        public ObservableCollection<MessageViewModel> Messages { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

        }

        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            var username = string.IsNullOrWhiteSpace(UsernameTextBox.Text) ? "Anonymous" : UsernameTextBox.Text.Trim();
            var serverUrl = $"http://localhost:5014/chathub?username={username}";

            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                return;
            }

            _connection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<User>("SetCurrentUser", user =>
            {
                _currentUserId = user.Id;
                Dispatcher.Invoke(() =>
                {
                    SendBtn.IsEnabled = true;
                });
            });


            _connection.On<int>("UserCount", count =>
            {
                Dispatcher.Invoke(() => UserCountTextBlock.Text = count.ToString());
            });

            _connection.On<ChatMessage>("ReceiveMessage", msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    var userName = "System";

                    if (msg.UserId.HasValue)
                    {
                        var user = MessagesUsersCache.FirstOrDefault(u => u.Id == msg.UserId.Value);
                        if (user == null)
                        {
                            var users = _connection.InvokeAsync<User[]>("GetUsersAsync").Result;
                            MessagesUsersCache = users.ToList();
                            user = MessagesUsersCache.FirstOrDefault(u => u.Id == msg.UserId.Value);
                        }

                        if (user != null)
                            userName = user.UserName;
                    }

                    Messages.Add(new MessageViewModel
                    {
                        Username = userName,
                        Text = msg.Text,
                        Timestamp = msg.Timestamp,
                        ItIsYou = msg.UserId == _currentUserId
                    });
                });
            });

            try
            {
                await _connection.StartAsync();
                ConnectBtn.IsEnabled = false;
                DisconnectBtn.IsEnabled = true;
                SendBtn.IsEnabled = true;

                await TryLoadHistoryViaSignalR();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}");
            }
        }

        private async Task TryLoadHistoryViaSignalR()
        {
            if (_connection == null) return;

            try
            {
                var messages = await _connection.InvokeAsync<ChatMessage[]>("GetHistoryAsync");
                var users = await _connection.InvokeAsync<User[]>("GetUsersAsync");
                MessagesUsersCache = users.ToList();
                Dispatcher.Invoke((Delegate)(() =>
                {
                    foreach (var msg in messages.OrderBy<ChatMessage, DateTime>(m => m.Timestamp))
                    {
                        var user = users.FirstOrDefault<User>(u => u.Id == msg.UserId);
                        string userName = user?.UserName ?? "System";
                        Messages.Add(new MessageViewModel
                        {
                            Username =  userName,
                            Text = msg.Text,
                            Timestamp = msg.Timestamp,
                            ItIsYou = msg.UserId == _currentUserId
                        });
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history: {ex.Message}");
            }
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessage();
                e.Handled = true;
            }
        }

        private async Task SendMessage()
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected) return;
            var text = MessageTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            var username = string.IsNullOrWhiteSpace(UsernameTextBox.Text) ? "Anonymous" : UsernameTextBox.Text.Trim();

            if (_currentUserId == Guid.Empty)
            {
                MessageBox.Show("User ID not set yet. Wait for connection.");
                return;
            }

            try
            {
                await _connection.InvokeAsync("SendMessage", _currentUserId, text);
                MessageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Send error: {ex.Message}");
            }
        }

        private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;

                ConnectBtn.IsEnabled = true;
                DisconnectBtn.IsEnabled = false;
                SendBtn.IsEnabled = false;
                UserCountTextBlock.Text = "0";
                Messages.Clear();
                MessageTextBox.Clear();
            }
        }
    }
}
