using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;


namespace ChatAppFrontend
{
    public partial class MainWindow : Window
    {
        // Create a single instance of HttpClient to manage requests to the backend
        private static readonly HttpClient client = new HttpClient();

        // List to hold chat messages
        private List<ChatMessage> messages = new List<ChatMessage>();

        // Flag to check offline status
        private bool isOffline = false;

        // List to store IDs of messages deleted while offline
        private List<int> offlineDeletedIds = new List<int>();

        // Moch receiver name
        private const string MockReceiver = "TestUser";

        public MainWindow()
        {
            InitializeComponent();

            // Bind the ListBox to the messages list
            ChatDisplay.ItemsSource = messages;

            // Initially hide the delete button
            DeleteButton.Visibility = Visibility.Collapsed;

            // Check server status upon app startup
            CheckServerStatus();

            // Fetch existing messages from the server
            _ = FetchMessages();

            // Load any previously stored offline deleted message IDs
            LoadOfflineDeletedIds();
        }

        /// <summary>
        /// Checks the server's staus to determine if the app is online or offline
        /// If offline, updates the app title and prevents server-dependent operations
        /// </summary>
        private async Task<bool> CheckServerStatus()
        {
            try
            {
                var response = await client.GetAsync("http://localhost:8000/status");

                if (response.IsSuccessStatusCode)
                {
                    this.Title = "ChatApp";
                    isOffline = false;

                    // Sync messages that were deleted while offline
                    await SyncOfflineDeletedMessages();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking server status: {ex.Message}");

                MessageBox.Show("Unable to connect to the server. You are offline.",
                                "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


            this.Title = "You are currently offline";
            isOffline = true;
            return false;
        }

        /// <summary>
        /// Syncs messages that were deleted while offline with the server
        /// Removes messages from the server if their deletion was successful
        /// </summary>
        private async Task SyncOfflineDeletedMessages()
        {
            foreach (int id in offlineDeletedIds.ToList())
            {
                if (id == -1) continue; // Skip invalid IDs

                try
                {
                    string url = $"http://localhost:8000/api/delete/{id}";
                    var deleteResponse = await client.DeleteAsync(url);

                    if (deleteResponse.IsSuccessStatusCode)
                    {
                        offlineDeletedIds.Remove(id);
                    }
                }
                catch
                {
                    MessageBox.Show($"Failed to delete message with ID {id} on the server.",
                                    "Sync Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// Fetches the chat history from the server and updates the chat display
        /// Ensures no blank messages are added to the chat
        /// </summary>
        private async Task FetchMessages()
        {
            try
            {
                string url = $"http://localhost:8000/api/history?sender=You&receiver={MockReceiver}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var messagesList = JsonSerializer.Deserialize<List<ChatMessage>>(responseData);

                    messages.Clear();
                    if (messagesList != null && messagesList.Count > 0)
                    {
                        foreach (var message in messagesList)
                        {
                            if (!string.IsNullOrWhiteSpace(message.Message))
                            {
                                messages.Add(new ChatMessage
                                {
                                    Id = message.Id,
                                    Message = message.Message,
                                    Sender = message.Sender,
                                    Timestamp = message.Timestamp,
                                    BackgroundColor = message.Sender == "You" ? "#A5B4FC" : "#FCE7F3",
                                    ForegroundColor = message.Sender == "You" ? "White" : "#6B7280",
                                    Alignment = message.Sender == "You" ? HorizontalAlignment.Right : HorizontalAlignment.Left
                                });
                            }
                        }
                    }
                    ChatDisplay.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("Failed to fetch messages from the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Request Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle the key down event for the message input box
        /// Applies a visual effect to the the Send button when the Enter key is pressed
        /// </summary>
        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton.RenderTransform = new ScaleTransform(0.95, 0.95);
                SendButton.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                SendButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D83F2"));
            }
        }

        /// <summary>
        /// Handles the key up event for the message input box
        /// Resets the visual effect on the Send button when the Enter key is released
        /// </summary>
        private async void MessageInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton.RenderTransform = new ScaleTransform(1, 1);
                SendButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5B4FC"));

                await SendMessage();
            }
        }

        /// <summary>
        /// Handles the click event for the Send button to send a message
        /// </summary>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        /// <summary>
        /// Refreshes the chat display by checking the server status and fetching messages
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            bool isOnline = await CheckServerStatus();
            if (isOnline)
            {
                await FetchMessages();
            }
            else
            {
                MessageBox.Show("You are offline. Unable to refresh messages.", "Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the selection change in the chat display and shows/hides the delete button
        /// </summary>
        private void ChatDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatDisplay.SelectedItem != null)
            {
                DeleteButton.Visibility = Visibility.Visible;
            }
            else
            {
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Deletes the selected message from the chat and optionally syncs the deletion with the server
        /// </summary>
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChatDisplay.SelectedItem is ChatMessage selectedMessage)
            {
                messages.Remove(selectedMessage);
                ChatDisplay.Items.Refresh();

                offlineDeletedIds.Add(selectedMessage.Id);
                SaveOfflineDeletedIds();


                if (!isOffline)
                {
                    try
                    {
                        string url = $"http://localhost:8000/api/delete/{selectedMessage.Id}";
                        var response = await client.DeleteAsync(url);


                        if (!response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Message could not be deleted on the server.",
                                "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("You are offline. Message deleted locally.",
                            "Offline", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Message deleted locally. Server not connected.",
                        "Offline", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Adds a new message to the chat display
        /// </summary>
        private void AddMessage(string message, bool isSent, int id = -1)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            messages.Add(new ChatMessage
            {
                Id = id,
                Message = message,
                Sender = isSent ? "You" : MockReceiver,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                BackgroundColor = isSent ? "#A5B4FC" : "#FCE7F3",
                ForegroundColor = isSent ? "White" : "#6B7280",
                Alignment = isSent ? HorizontalAlignment.Right : HorizontalAlignment.Left
            });

            ChatDisplay.Items.Refresh();
            ChatDisplay.ScrollIntoView(ChatDisplay.Items[ChatDisplay.Items.Count - 1]);
        }

        /// <summary>
        /// Sends a message to the server and adds it to the local chat display
        /// If the server responds successfully, it updates the message ID and processes the server's response
        /// </summary>
        private async Task SendMessage()
        {
            string userMessage = MessageInput.Text;

            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                var tempMessage = new ChatMessage
                {
                    Id = -1, // Temporary invalid ID
                    Message = userMessage,
                    Sender = "You",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    BackgroundColor = "#A5B4FC",
                    ForegroundColor = "White",
                    Alignment = HorizontalAlignment.Right
                };
                messages.Add(tempMessage);
                ChatDisplay.Items.Refresh();
                MessageInput.Clear();

                try
                {
                    string url = "http://localhost:8000/api/send";
                    var messagePayload = new
                    {
                        sender = "You",
                        receiver = MockReceiver,
                        message = userMessage
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(messagePayload),
                        Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

                        if (responseData.TryGetValue("id", out object messageIdObj) &&
                            int.TryParse(messageIdObj.ToString(), out int messageId))
                        {
                            tempMessage.Id = messageId;
                        }

                        if (responseData.TryGetValue("response", out object serverResponse))
                        {
                            AddMessage(serverResponse.ToString(), false);
                        }
                    }
                    else
                    {
                        AddMessage("Server: Message could not be sent.", false);
                    }
                }
                catch
                {
                    AddMessage("Server: You are offline. Message could not be sent.", false);
                }
            }
        }

        /// <summary>
        /// Saves the IDs of messages deleted while offline to a file
        /// </summary>
        private void SaveOfflineDeletedIds()
        {
            File.WriteAllText("offline_deleted.json", JsonSerializer.Serialize(offlineDeletedIds));
        }

        /// <summary>
        /// Loads the IDs of messages deleted while offline from a file
        /// </summary>
        private void LoadOfflineDeletedIds()
        {
            if (File.Exists("offline_deleted.json"))
            {
                var content = File.ReadAllText("offline_deleted.json");
                offlineDeletedIds = JsonSerializer.Deserialize<List<int>>(content) ?? new List<int>();
            }
            else
            {
                offlineDeletedIds = new List<int>();
                SaveOfflineDeletedIds();
            }
        }
    }
}