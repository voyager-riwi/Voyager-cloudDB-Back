namespace CrudCloudDb.Application.DTOs.Credential
{
    /// <summary>
    /// Resultado de generación de credenciales
    /// </summary>
    public class CredentialsResult
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}