﻿namespace Data
{
    /// <summary>
    /// A single chat message for various purposes
    /// </summary>
    public class ChatMessage
    {
        public required string Sender { get; set; }
        //new
        public required ConsoleColor SenderColor { get; set; }
        public required string Content { get; set; }
    }
}