namespace CrudCloudDb.Application.DTOs.Email
{
    public class DatabaseCreatedEmailDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}