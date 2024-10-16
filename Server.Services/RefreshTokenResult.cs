namespace Server.Services;

public class RefreshTokenResult
{
    public bool Succeeded { get; set; }
    
    public string? Token { get; set; }
    
    public string? RefreshToken { get; set; }
    
    public string? Error { get; set; }
    
}