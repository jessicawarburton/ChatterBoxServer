namespace Server.Services;

public class MessageResponse
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    
    public string SenderId { get; set; }
    public string SenderUsername { get; set; }
    
    
    
}