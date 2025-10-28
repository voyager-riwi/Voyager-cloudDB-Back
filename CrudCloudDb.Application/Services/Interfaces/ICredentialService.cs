// CrudCloudDb.Application/Services/Interfaces/ICredentialService.cs

namespace CrudCloudDb.Application.Services.Interfaces
{
    /// <summary>
    /// Interfaz para servicio de generación de credenciales
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Genera credenciales aleatorias seguras
        /// </summary>
        Task<CredentialResult> GenerateCredentialsAsync();
    }

    /// <summary>
    /// Resultado de generación de credenciales
    /// </summary>
    public class CredentialResult
    {
        /// <summary>
        /// Username generado (ej: "kx9f2q8p1m4n")
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Password en texto plano (solo para mostrar al usuario UNA vez)
        /// NO guardar esto en la base de datos
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Hash BCrypt del password (esto SÍ se guarda en BD)
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;
    }
}
