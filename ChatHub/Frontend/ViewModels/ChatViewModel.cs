using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Frontend.Model;
using FrontEnd.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Windows;


namespace Frontend.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private HubConnection? _connection;
        private Guid _currentUserId;
        private List<User> _usersCache = new();

        [ObservableProperty] private string username = "Anonymous";
        [ObservableProperty] private string messageText = "";
        [ObservableProperty] private bool isConnected;


        public ObservableCollection<MessageViewModel> Messages { get; } = new();
        public IRelayCommand MinimizeCommand { get; }
        public IRelayCommand CloseCommand { get; }

        public ChatViewModel()
        {
            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            SendCommand = new AsyncRelayCommand(SendMessageAsync, CanSend);
            DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);

            MinimizeCommand = new RelayCommand(() =>
            {
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
            });

            MainWindow.OnClosed += MainWindow_OnClosed;
        }


        public void Dispose()
        {
            MainWindow.OnClosed -= MainWindow_OnClosed;
        }

        public IAsyncRelayCommand ConnectCommand { get; }
        public IAsyncRelayCommand SendCommand { get; }
        public IAsyncRelayCommand DisconnectCommand { get; }

        private async void MainWindow_OnClosed()
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await DisconnectAsync();
            });
        }


        private bool CanSend() =>
            IsConnected && !string.IsNullOrWhiteSpace(MessageText);

        private async Task ConnectAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
                return;

            _connection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:5014/chathub?username={Username}")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<User>("SetCurrentUser", user =>
            {
                _currentUserId = user.Id;
                IsConnected = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SendCommand.NotifyCanExecuteChanged();
                });
            });

            _connection.On<ChatMessage>("ReceiveMessage", msg =>
            {
                var userName = "System";
                if (msg.UserId.HasValue)
                {
                    var user = _usersCache.FirstOrDefault(u => u.Id == msg.UserId.Value);
                    if (user == null)
                    {
                        var users = _connection.InvokeAsync<User[]>("GetUsersAsync").Result;
                        _usersCache = users.ToList();
                        user = _usersCache.FirstOrDefault(u => u.Id == msg.UserId.Value);
                    }
                    if (user != null)
                        userName = user.UserName;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(new MessageViewModel
                    {
                        Username = userName,
                        Text = msg.Text,
                        Timestamp = msg.Timestamp,
                        ItIsYou = msg.UserId == _currentUserId,
                        AvatarImage = msg.UserId == _currentUserId
                            ? "https://www.pngplay.com/wp-content/uploads/12/User-Avatar-Profile-Transparent-Clip-Art-PNG.png"
                            : "https://cdn-icons-png.flaticon.com/512/10337/10337609.png"
                    });
                });
            });

            try
            {
                await _connection.StartAsync();
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}");
            }
        }

        private async Task LoadHistoryAsync()
        {
            if (_connection == null) return;

            try
            {
                var messages = await _connection.InvokeAsync<ChatMessage[]>("GetHistoryAsync");
                var users = await _connection.InvokeAsync<User[]>("GetUsersAsync");
                _usersCache = users.ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var msg in messages.OrderBy(m => m.Timestamp))
                    {
                        var user = users.FirstOrDefault(u => u.Id == msg.UserId);
                        string userName = user?.UserName ?? "System";
                        Messages.Add(new MessageViewModel
                        {
                            Username = userName,
                            Text = msg.Text,
                            Timestamp = msg.Timestamp,
                            ItIsYou = msg.UserId == _currentUserId,
                            AvatarImage = msg.UserId == _currentUserId
                                ? "https://www.pngplay.com/wp-content/uploads/12/User-Avatar-Profile-Transparent-Clip-Art-PNG.png"
                                : "https://cdn-icons-png.flaticon.com/512/10337/10337609.png"
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history: {ex.Message}");
            }
        }

        private async Task SendMessageAsync()
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected) return;
            if (string.IsNullOrWhiteSpace(MessageText)) return;

            try
            {
                await _connection.InvokeAsync("SendMessage", _currentUserId, MessageText);
                MessageText = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Send error: {ex.Message}");
            }
        }

        partial void OnMessageTextChanged(string oldValue, string newValue)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SendCommand.NotifyCanExecuteChanged();
            });
        }

        partial void OnIsConnectedChanged(bool oldValue, bool newValue)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SendCommand.NotifyCanExecuteChanged();
            });
        }

        private async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;

                IsConnected = false;
                Messages.Clear();
                MessageText = "";
                SendCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
