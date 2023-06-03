using Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace Client;

/// <summary>
/// A client for the simple web server
/// </summary>
public class ChatClient
{
    /// <summary>
    /// The HTTP client to be used throughout
    /// </summary>
    private readonly HttpClient httpClient;

    /// <summary>
    /// The alias of the user
    /// </summary>
    private readonly string alias;

    /// <summary>
    /// The color of the user
    /// </summary>
    public ConsoleColor userColor { get; private set; }


    /// <summary>
    /// The cancellation token source for the listening task
    /// </summary>
    readonly CancellationTokenSource cancellationTokenSource = new();

    //new
    /// <summary>
    /// The dictionary save the names and colors of users for private chat . 
    /// </summary>
    public Dictionary<string, ConsoleColor> usernameColors
    {
        get; set;
    }



    /// <summary>
    /// Initializes a new instance of the <see cref="ChatClient"/> class.
    //new/// This method generates the colors for client
    /// </summary>
    /// <param name="alias">The alias of the user.</param>
    /// <param name="serverUri">The server URI.</param>
    public ChatClient(string alias, Uri serverUri)
    {  
        ConsoleColor userColorGenerated = userColorGenerate();
        this.userColor = userColorGenerated;
        this.alias = alias;
        this.httpClient = new HttpClient();
        this.httpClient.BaseAddress = serverUri;

    }

    //new This method generates the colors for client
    public ConsoleColor userColorGenerate()
    {
        var random = new Random();
        List<ConsoleColor> excludedColors = new List<ConsoleColor>();
        excludedColors.Add(ConsoleColor.White);
        excludedColors.Add(ConsoleColor.Black);
        var availableColors = Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>().ToList();
        var colorwithoutblackandWhite = availableColors.Except(excludedColors).ToList();
        ConsoleColor userColor = colorwithoutblackandWhite[random.Next(colorwithoutblackandWhite.Count)];
        return userColor;
    }

    /// <summary>
    /// Connects this client to the server.
    /// </summary>
    /// <returns>True if the connection could be established; otherwise False</returns>
    public async Task<bool> Connect()
    {
        // create and send a welcome message
        var message = new ChatMessage { Sender = this.alias, SenderColor = this.userColor, Content = $"Hi, I joined the chat!" };
        var response = await this.httpClient.PostAsJsonAsync("/messages", message);

        return response.IsSuccessStatusCode;
    }

    //new //check if the name or color of user is already taken
    public async Task<HttpStatusCode> Check()
    {
        // create and send a welcome message
        var message = new ChatMessage { Sender = this.alias, SenderColor = this.userColor, Content = "Hi here Methode for controll the name and color of register" };
        var response = await this.httpClient.PostAsJsonAsync($"/messages/id", message);

        return response.StatusCode;
    }

    /// <summary>
    /// Sends a new message into the chat.
    /// </summary>
    /// <param name="content">The message content as text.</param>
    /// <returns>True if the message could be send; otherwise False</returns>
    public async Task<bool> SendMessage(string content)
    {
        // creates the message and sends it to the server
        var message = new ChatMessage { Sender = this.alias, SenderColor = this.userColor, Content = content };
        var response = await this.httpClient.PostAsJsonAsync($"/messages", message);

        return response.IsSuccessStatusCode;
    }

    // new // i tried to solve
    public async Task<bool> SendPrivateMessage(string username, string content)
    {
        // creates the message and sends it to the server
        var message = new ChatMessage { Sender = username, SenderColor = this.userColor, Content = content };
        var response = await this.httpClient.PostAsJsonAsync($"/messages/username/content", message);

        return response.IsSuccessStatusCode;
    }
    //new  
    /// <summary>
    /// deleltes the client when he cancels
    /// </summary>
    /// <returns>True if the user cancels; otherwise False</returns>
    public async Task<bool> Console_Cancel()
    {
        var message = new ChatMessage { Sender = this.alias, SenderColor = this.userColor, Content = $"Hi, I joined the chat!" };
        var response = await this.httpClient.PostAsJsonAsync("/messages/usercolor", message);

        return response.IsSuccessStatusCode;
    }

    // new // i tried to solve
    public async Task PrivateList()
    {

        var cancellationToken = this.cancellationTokenSource.Token;
        Dictionary<string, ConsoleColor> usernameDice = this.usernameColors;
        try
        {

            usernameDice = await this.httpClient.GetFromJsonAsync<Dictionary<string, ConsoleColor>>($"/messages?id={this.alias}&usercolor={this.userColor}", cancellationToken);
            if (usernameDice != null)
            {
                Console.WriteLine("The available user are :");
                foreach ((var id, var color) in usernameDice)
                {
                    Console.WriteLine(id);

                }
            }
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // catch the cancellation 
            this.OnMessageReceived("Me", "Leaving the chat", ConsoleColor.White);

        }
    }


    /// <summary>
    /// Listens for messages until this process is cancelled by the user.
    /// </summary>
    public async Task ListenForMessages()
    {
        var cancellationToken = this.cancellationTokenSource.Token;

        // run until the user request the cancellation
        while (true)
        {
            try
            {
                // listening for messages. possibly waits for a long time.
                var message = await this.httpClient.GetFromJsonAsync<ChatMessage>($"/messages?id={this.alias}&usercolor={this.userColor}", cancellationToken);

                // if a new message was received notify the user
                if (message != null)
                {
                    this.OnMessageReceived(message.Sender, message.Content, message.SenderColor);
                    //new // to organize on console
                    Console.WriteLine("Geben Sie bitte einfach Ihre '(Nachricht)' ein , '(private)' um private Nachrichten zu senden oder '(exit)' zum Beenden: ");
                    Console.ForegroundColor = message.SenderColor;
                }
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // catch the cancellation 
                this.OnMessageReceived("Me", "Leaving the chat", ConsoleColor.White);
                break;
            }
        }
    }

    /// <summary>
    /// Cancels the loop for listening for messages.
    /// </summary>
    public void CancelListeningForMessages()
    {
        // signal the cancellation request
        this.cancellationTokenSource.Cancel();
    }
    //new i tried to solve
    public async void PrivatChat(string alias, string content)
    {
        await this.SendPrivateMessage(alias, content);
    }

    //new // 
    /// <summary>
    /// filter the swear word when the user writes swear word 
    /// </summary>
    /// <param name="content">the massege content as text.</param>
    /// <returns></returns>
    public string filterSwearWord(string content)
    {
        //please be careful that the path needs to be changed
        string filePath = "C:\\Users\\abodh\\OneDrive\\Dokumente\\Semester 4\\PVS\\puvsBasicHttpChat\\Client\\schimpfwort.txt";
        string newcontent = "";
        if (File.Exists(filePath))
        {
            string? line;
            try
            {
                StreamReader sr = new StreamReader(filePath);
               
                line = sr.ReadLine();
                bool swearword = false; 
                while (line != null)
                {
                   if (content.Contains(line))
                    {
                        swearword = true;
                        newcontent = content.Replace(line, "*******");
                        content = newcontent;
                    }
                     line = sr.ReadLine();
                     
                }
                if (swearword)
                {
                    Console.WriteLine("your message contains swear words , unfortunately I'm forced to filter");

                }
                sr.Close();
                return content;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }
        }
        else
        {
            Console.WriteLine("Die Datei existiert nicht.");
        }
        return content;
    }
   // Enabled the user to receive new messages. The assigned delegated is called when a new message is received.
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Called when a message was received and signal this to the user using the MessageReceived event.
    /// </summary>
    /// <param name="sender">The alias of the sender.</param>
    /// <param name="message">The containing message as text.</param>
    protected virtual void OnMessageReceived(string sender, string message, ConsoleColor usernamecolor)
    {
        this.MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Sender = sender, Message = message, Sendercolor = usernamecolor });
    }

}