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
using System.Windows.Input;
using System.Collections.Generic;

namespace WPFServer
{
    public partial class MainWindow : Window
    {
        private List<TcpClient> clients = new List<TcpClient>();
        private Dictionary<TcpClient, string> clientIdentifiers = new Dictionary<TcpClient, string>();
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
            int clientCount = 1;
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                clients.Add(client);
                ClientCounting.Text = clientCount.ToString();
                string clientName = "Client "+ clientCount;
                clientCount++;
                clientIdentifiers[client] = clientName;

                AddMessageToChat($"{clientName} connected", false, true);
                bool firstTime = true;
                WriteToClient("You are " + clientName, client, firstTime);
                _ = HandleClient(client);
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0) break;  

                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);
                string clientName = clientIdentifiers[client];
                if (message.ToLower() == "exit")
                {
                    clients.Remove(client);
                    AddMessageToChat($"{clientName} disconnected", false, true);
                    break;
                }
                string messageWithClient = clientName + ": " + message;
                AddMessageToChat(messageWithClient, false);  
                WriteToClient(messageWithClient, client); 
            }

            client.Close();
        }
        private void WriteToClient(string message, TcpClient sender, bool firstTime = false)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);

            if (firstTime)
            {
                foreach (var client in clients)
                {
                    if (client == sender)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            else {
                foreach (var client in clients)
                {
                    if (client != sender)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            
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
