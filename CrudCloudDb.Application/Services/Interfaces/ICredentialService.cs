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
    }
}
