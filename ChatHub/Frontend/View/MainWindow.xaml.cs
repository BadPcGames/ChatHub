using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FrontEnd.ViewModels;
using Frontend.Model;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Frontend.ViewModels;
using System.Threading.Channels;
using System.ComponentModel;

namespace Frontend
{
    public partial class MainWindow : Window
    {
        public delegate void Closed();
        public static event Closed OnClosed;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ChatViewModel();
        }

        private void Widow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OnClosed?.Invoke();
            Close();
        }
    }
}
    