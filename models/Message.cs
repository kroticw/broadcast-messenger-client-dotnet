using System;

namespace models;

public class Message
{
    public User Sender { get; set; }
    public string Text { get; set; }
    public DateTime Timestamp { get; set; }
}