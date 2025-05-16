public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiration { get; set; }

    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? UserType { get; set; }
    public string? FullName { get; set; }
    public string? Message { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
}