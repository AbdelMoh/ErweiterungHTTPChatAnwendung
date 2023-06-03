using Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace Server;

/// <summary>
/// This is a very basic implementation of a chat server.
/// There are lot of things to improve...
/// </summary>
public class ChatServer
{
    /// <summary>
    /// The message history
    /// </summary>
    public readonly ConcurrentQueue<ChatMessage> messageQueue = new();

    /// <summary>
    /// All the chat clients
    /// </summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ChatMessage>> waitingClients = new();


    private readonly Dictionary<string, ConsoleColor> usernameColors = new();



    /// <summary>
    /// The lock object for concurrency
    /// </summary>
    private readonly object lockObject = new();

    /// <summary>
    /// Configures the web services.
    /// </summary>
    /// <param name="app">The application.</param>
    /// 
    public void Configure(IApplicationBuilder app)
    {

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {   //new // for checking whether the client is already assigned
            endpoints.MapPost("/messages/id", async context =>
            {

                var message = await context.Request.ReadFromJsonAsync<ChatMessage>();

                var already = false;
                lock (lockObject)
                {
                    if (string.IsNullOrEmpty(message?.Sender))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        Console.WriteLine("Name is required.");

                    }
                    else if (this.usernameColors.ContainsKey(message.Sender) || (this.usernameColors.ContainsValue(message.SenderColor)))
                    {
                        Console.WriteLine("the 'name' or 'color' of user is taken");
                        already = true;
                    }
                    else
                    {
                        this.usernameColors.Add(message.Sender, message.SenderColor);
                        Console.WriteLine($"Client '{message.Sender}' added to UsernameColorDict");

                    }

                }

                if (already == true)
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    await context.Response.WriteAsync("Name is already taken.");

                }
                else
                {
                    await context.Response.WriteAsJsonAsync("registered seccussfully");

                }
            }); 
            // new // for delete when the user cancels 
            endpoints.MapPost("/messages/usercolor",async context =>
            {
               var message = await context.Request.ReadFromJsonAsync<ChatMessage>();
                if (message != null)
                {
                    if (this.usernameColors.ContainsKey(message.Sender) || (this.usernameColors.ContainsValue(message.SenderColor)))
                    {
                        this.usernameColors.Remove(message.Sender);
                        Console.WriteLine($"{message.Sender} is deleted");
                    }
                }
            });



        });



        // The endpoint to register a client to the server to subsequently receive the next message
        // This endpoint utilizes the Long-Running-Requests pattern.


        app.UseEndpoints(endpoints =>
        {

            endpoints.MapGet("/messages", async context =>
            {


                var tcs = new TaskCompletionSource<ChatMessage>();

                context.Request.Query.TryGetValue("id", out var rawId);
                var id = rawId.ToString();

             

                Console.WriteLine($"Client '{id}' registered");




                // register a client to receive the next message

                var error = true;
                lock (this.lockObject)
                {
                    if (this.waitingClients.ContainsKey(id))
                    {
                        if (this.waitingClients.TryRemove(id, out _))
                        {
                            Console.WriteLine($"Client '{id}' removed from waiting clients");

                        }
                    }

                    if (this.waitingClients.TryAdd(id, tcs))
                    {
                        Console.WriteLine($"Client '{id}' added to waiting clients");

                        error = false;


                    }
                }
                //You could replace all of the above with just one line...
                //this.waitingClients.AddOrUpdate(id.ToString(), tcs, (_, _) => tcs);


                //if anything went wrong send out an error message
                if (error)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal server error.");
                }

                // otherwise wait for the next message broadcast
                var message = await tcs.Task;

                Console.WriteLine($"Client '{id}' received message: {message.Content}");

                // send out the next message
                await context.Response.WriteAsJsonAsync(message);
            });



            //This endpoint is for sending messages into the chat

            endpoints.MapPost("/messages", async context =>
           {
               var message = await context.Request.ReadFromJsonAsync<ChatMessage>();

               if (message == null)
               {
                   context.Response.StatusCode = StatusCodes.Status400BadRequest;
                   await context.Response.WriteAsync("Message invalid.");
               }

               Console.WriteLine($"Received message from client: {message!.Content}");

               // maintain the chat history
               this.messageQueue.Enqueue(message);

               // broadcast the new message to all registered clients
               lock (this.lockObject)
               {
                   foreach (var (id, client) in this.waitingClients)
                   {
                       Console.WriteLine($"Broadcasting to client '{id}'");

                       // possible memory leak as the 'dead' clients are never removed from the list
                       client.TrySetResult(message);

                   }
               }

               Console.WriteLine($"Broadcasted message to all clients: {message.Content}");

               // confirm that the new message was successfully processed
               context.Response.StatusCode = StatusCodes.Status201Created;
               await context.Response.WriteAsync("Message received and processed.");
           });
        });



    }

}