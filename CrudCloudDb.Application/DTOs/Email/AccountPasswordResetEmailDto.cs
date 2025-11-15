namespace CrudCloudDb.Application.DTOs.Email
{
    public class AccountPasswordResetEmailDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty; // Este será nuestro token de 6 dígitos
    }
}