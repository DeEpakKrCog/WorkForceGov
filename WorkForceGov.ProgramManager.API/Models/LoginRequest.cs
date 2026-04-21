namespace WorkForceGovProject.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserRole { get; set; } = string.Empty;
    }
}
