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
using System.Xml.Linq;
using System.Linq;

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
            AddMessageToChat("Waiting for client", true);
            int clientCount = 1;
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                clients.Add(client);
                ClientCounting.Text = clientCount.ToString();
                clientCount++;
                await ReceiveName(client);
                _ = HandleClient(client);
            }
        }
        private async Task ReceiveName(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            string message = Encoding.ASCII.GetString(buffer, 0, byteCount);//receive name

            if (message.ToLower() == "exit")
            {
                clients.Remove(client);
                string clientName = clientIdentifiers[client]; // Get the client name for the message
                clientIdentifiers.Remove(client);//look here
                AddMessageToChat($"{clientName} disconnected", true);
                BroadcastClientList();//look here
            }
            else
            {
                clientIdentifiers[client] = message;
                AddMessageToChat($"{message} connected", true);
                BroadcastClientList();
            }
        }
        private void BroadcastClientList(/*TcpClient client = null*/)//this broacast connected client
        {
            var clientList = clientIdentifiers.Values.ToList();
            string clientListString = string.Join(",", clientList);
            foreach (var client in clients)
            {
                NetworkStream stream = client.GetStream();
                byte[] data;
                data = Encoding.ASCII.GetBytes("clientlist:" + clientListString);
                stream.Write(data, 0, data.Length);
            }
        }
        private async Task HandleClient(TcpClient sender)
        {
            NetworkStream stream = sender.GetStream();
            while (true)
            {
                byte[] buffer = new byte[1024];
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0) break;  // Client disconnected

                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);
                string senderName = clientIdentifiers[sender];

                if (message.ToLower() == "exit")
                {
                    clients.Remove(sender);
                    clientIdentifiers.Remove(sender);
                    AddMessageToChat($"{senderName} disconnected", true);
                    BroadcastClientList();
                    break;
                }

                // Split the message by the delimiter
                string[] parts = message.Split(new string[] { "@->@" }, StringSplitOptions.None);
                if (parts.Length == 2) // Ensure there is a recipient and a message
                {
                    string recipient = parts[0].Trim();
                    string textMessage = parts[1].Trim();
                    string textMessageWithRecipient = $"{senderName}: {textMessage}";

                    // Display the message in the server's chat
                    AddMessageToChat(textMessageWithRecipient, false);

                    // Send the message to the targeted client
                    byte[] data = Encoding.ASCII.GetBytes(textMessageWithRecipient);
                    bool messageSent = false;

                    foreach (var client in clients)
                    {
                        string clientName = clientIdentifiers[client];
                        if (clientName.Equals(recipient))
                        {
                            try
                            {
                                NetworkStream streamC = client.GetStream();
                                await streamC.WriteAsync(data, 0, data.Length); // Use async write
                                messageSent = true;
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions (like client disconnects)
                                AddMessageToChat($"Error sending message to {recipient}: {ex.Message}", false);
                            }
                            break; // Exit loop after sending message to the intended recipient
                        }
                    }

                    // Optionally handle the case where the recipient is not found
                    if (!messageSent)
                    {
                        AddMessageToChat($"Recipient {recipient} not found.", false);
                    }
                }
                else
                {
                    AddMessageToChat("Message format is incorrect.", false);
                }
            }

            sender.Close();
        }

        private async Task qHandleClient(TcpClient sender)
        {
            NetworkStream stream = sender.GetStream();
            while (true)
            {
                byte[] buffer = new byte[1024];
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0) break;  

                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);
                string senderName = clientIdentifiers[sender];
                string[] parts = message.Split(new string[] { "@->@" }, StringSplitOptions.None);
                if (parts.Length == 2) 
                {
                    string recipient = parts[0].Trim();
                    string textMessage = parts[1];
                    string textMessageWithRecipient = senderName + ": " + textMessage;
                    if (message.ToLower() == "exit")
                    {
                        clients.Remove(sender);
                        clientIdentifiers.Remove(sender);//look here
                        AddMessageToChat($"{senderName} disconnected", true);
                        BroadcastClientList();
                        break;
                    }
                    string messageWithClient = senderName + ": " + textMessage;
                    AddMessageToChat(messageWithClient, false);

                    //write message to targeed client
                    byte[] data = Encoding.ASCII.GetBytes(textMessageWithRecipient);
                    foreach (var client in clients)
                    {
                        string clientName = clientIdentifiers[client];
                        if (clientName.Equals(recipient))
                        {
                            NetworkStream streamC = client.GetStream();
                            streamC.Write(data, 0, data.Length);
                        }
                    }

                    //WriteToClient(messageWithClient, sender, recipient); 
                }
                else { MessageBox.Show("WTH"); }

            }

            sender.Close();
        }
        private void WriteToClient(string message, TcpClient sender, string recipient)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);

            foreach (var client in clients)
            {
                string clientName = clientIdentifiers[client];
                if (clientName.Equals(recipient))
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
            }

        }

        private void AddMessageToChat(string message, bool justConnect = false)
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
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(1),
                    MaxWidth = 300
                };
            }

            ChatDisplay.Children.Add(messageBlock);
            ChatScrollViewer.ScrollToEnd();
        }
    }
}
