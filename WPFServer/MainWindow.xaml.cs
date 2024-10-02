using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;

namespace WPFServer
{
    public partial class MainWindow : Window
    {
        private NetworkStream stream;
        public MainWindow()
        {
            InitializeComponent();
            _ = ConnectWithServer();
        }

        private async Task ConnectWithServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 12345);
            listener.Start();
            AddMessageToChat("Waiting for client", false, true);

            TcpClient client = await listener.AcceptTcpClientAsync();
            AddMessageToChat("Client connected", false, true);
            this.stream = client.GetStream();

            await Task.WhenAll(ReadFromClient(stream));
           
            client.Close();
            listener.Stop();
            Console.WriteLine("Server has stopped.");
        }

        private async Task ReadFromClient(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                string textFromClient = Encoding.ASCII.GetString(buffer, 0, byteCount);

                if (textFromClient.ToLower() == "exit")
                {
                    Application.Current.Shutdown();
                    break;
                }

                Dispatcher.Invoke(() => AddMessageToChat(textFromClient, false));
            }
        }
        
        private void SendBtnClicked(object sender, RoutedEventArgs e)
        {
            string userInput = UserInput.Text.ToString();
            if (string.IsNullOrEmpty(userInput)) return;
            if (userInput.ToLower() == "exit")
            {
                Application.Current.Shutdown();
            }
            

            AddMessageToChat(userInput, true);//add the message into ui
            byte[] data = Encoding.ASCII.GetBytes(userInput);
            stream.Write(data, 0, data.Length);//send message to server
            if (userInput.ToLower() == "exit")
            {
                stream.Close();
            }
            UserInput.Clear();
        }
        private void AddMessageToChat(string message, bool isClient, bool justConnect = false)
        {
            TextBlock messageBlock = new TextBlock();
            if (justConnect)
            {
                messageBlock = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(15),
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Black),
                    Foreground = Brushes.Yellow,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(1),
                    MaxWidth = 300,
                    FontSize = 10
                };
            }
            else
            {
                messageBlock = new TextBlock
                {
                    Text = message,
                    Margin = new Thickness(5),
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.LightBlue),
                    HorizontalAlignment = isClient ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                    Padding = new Thickness(1),
                    MaxWidth = 300
                };
            }

            ChatDisplay.Children.Add(messageBlock);
            ChatScrollViewer.ScrollToEnd();
        }
    }
}
