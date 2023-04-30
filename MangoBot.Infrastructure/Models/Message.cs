namespace MangoBot.Infrastructure.Models;

public class Message
{
    public string MessageBody { get; set; }

    public List<string>? Tags { get; set; }
    
    public string Channel { get; set; }
}