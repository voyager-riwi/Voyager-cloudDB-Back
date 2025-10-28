namespace CrudCloudDb.Application.DTOs.Email
{
    public class DatabaseDeletedEmailDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Engine { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
    }
}