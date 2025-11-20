using CrudCloudDb.Application.DTOs.Credential;

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
        Task<CredentialsResult> GenerateCredentialsAsync();

        /// <summary>
        /// Genera un nombre aleatorio para base de datos
        /// </summary>
        string GenerateDatabaseName();
    }
}