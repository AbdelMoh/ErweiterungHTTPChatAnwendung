using Data;
using System.Collections.Concurrent;
using System.Net;

namespace Client;

/// <summary>
/// A most basic chat client for the console
/// </summary>
public class Program
{

    //private static ConcurrentDictionary<string, TaskCompletionSource<ChatMessage>> userlist = new();

    public static async Task Main(string[] args)
    {
        var serverUri = new Uri("http://localhost:5000");

        // query the user for a name
        Console.Write("Geben Sie Ihren Namen ein: ");
        var sender = Console.ReadLine() ?? Guid.NewGuid().ToString();
        Console.WriteLine();



        // create a new client and connect the event handler for the received messages
        var client = new ChatClient(sender, serverUri);
        client.MessageReceived += MessageReceivedHandler;


        // connect to the server and start listening for messages
        var connectTask = await client.Connect();

        // if the name or color of user is already or the user registed without name
        var error = false;
        HttpStatusCode checkTask = await client.Check();
        // without name
        if (checkTask == HttpStatusCode.BadRequest)
        {
            Console.WriteLine("Name is required.");
            client.CancelListeningForMessages();
            error = true;

        }
        // the name or color is available
        if (checkTask == HttpStatusCode.Conflict)
        {
            Console.WriteLine("the 'username' oder 'usercolor' is already taken please try with another name again ");
            client.CancelListeningForMessages();
            error = true;

        }

        var listenTask = client.ListenForMessages();
        // query the user for messages to send or the exit command
        var filteredContent = "";
        Console.WriteLine("Geben Sie bitte einfach Ihre '(Nachricht)' ein , '(private)' um private Nachrichten zu senden oder '(exit)' zum Beenden: ");
        while ((true) && (!error))
        {   //make the color of console as color of user 
            Console.ForegroundColor = client.userColor;
            var content = Console.ReadLine() ?? string.Empty;
            Console.ResetColor();

            // cancel the listening task and exit the loop
            if (content.ToLower() == "exit")
            {
                client.CancelListeningForMessages();
                // delete the user when he cancels
                await client.Console_Cancel();
                break;
            }
            //new privat chat 
            if (content.ToLower() == "private")
            {
                //  client.PrivatChat();
            }
            else
            {
                //Call a method to filter the swearwords based on a file , see attachment file schimpfwort.txt
                filteredContent = client.filterSwearWord(content.ToLower());
                Console.WriteLine($"Sending message: {filteredContent}");
               
            }

            // send the message and display the result
            if (!content.Equals("privat"))
            {

                if (await client.SendMessage(filteredContent))
                {

                    Console.WriteLine("Message sent successfully.");

                }
                else
                {
                    Console.Write("Failed to send message.");
                }
            }


        }


        // wait for the listening for new messages to end
        await Task.WhenAll(listenTask);


        Console.WriteLine("\nGood bye...");

    }

    /// <summary>
    //new// /// Helper method to display the newly received messages with the time and color of user.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MessageReceivedEventArgs"/> instance containing the event data.</param>


    static void MessageReceivedHandler(object? sender, MessageReceivedEventArgs e)
    {
                  
        DateTime currentTime = DateTime.Now;
        string formattedTime = currentTime.ToString("HH:mm:ss"); // Beispiel: 15:30:45
        Console.ResetColor();
        Console.Write($"\nReceived new message from : ");
        Console.ForegroundColor = e.Sendercolor;
        Console.Write($"{e.Sender}:{e.Message} an Time {formattedTime}\n");
        Console.ResetColor();
        
    }

   
}