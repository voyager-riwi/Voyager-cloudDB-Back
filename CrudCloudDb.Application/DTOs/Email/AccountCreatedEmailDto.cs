namespace CrudCloudDb.Application.DTOs.Email
{
    /// <summary>
    /// DTO para email de bienvenida al crear cuenta
    /// </summary>
    public class AccountCreatedEmailDto
    {
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}