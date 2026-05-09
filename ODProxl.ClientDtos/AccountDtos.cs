namespace ODProxl.ClientDtos;

public record AccountDto
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public record LoginRequestDto
{
    public string Token { get; set; }
    public string Username { get; set; }
    public DateTime ExpiresAt { get; set; }
}
