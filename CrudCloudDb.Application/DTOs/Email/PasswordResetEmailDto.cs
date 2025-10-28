namespace CrudCloudDb.Application.DTOs.Email
{
    public class PasswordResetEmailDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;
        public string NewUsername { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public DateTime ResetAt { get; set; }
    }
}