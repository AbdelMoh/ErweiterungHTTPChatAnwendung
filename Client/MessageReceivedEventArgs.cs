namespace Client;

public class MessageReceivedEventArgs : EventArgs
{ 
    public required ConsoleColor Sendercolor { get; set; }
    public required string Sender { get; set; }
    public required string Message { get; set; }
}