// CrudCloudDb.Infrastructure/Services/CredentialService.cs

using Microsoft.Extensions.Logging;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Utilities;

namespace CrudCloudDb.Infrastructure.Services
{
    /// <summary>
    /// Servicio para generar credenciales completas para bases de datos.
    /// Combina generación de username/password con hashing seguro.
    /// </summary>
    public class CredentialService : ICredentialService
    {
        private readonly ILogger<CredentialService> _logger;

        public CredentialService(ILogger<CredentialService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Genera credenciales completas (username, password en texto plano, y hash)
        /// </summary>
        /// <returns>Objeto con username, password y passwordHash</returns>
        public async Task<CredentialResult> GenerateCredentialsAsync()
        {
            // Generar username aleatorio usando la utilidad de Miguel
            // Ejemplo resultado: "kx9f2q8p1m4n"
            var username = CredentialGenerator.GenerateUsername(12);
            
            // Generar password seguro usando la utilidad de Miguel
            // Ejemplo resultado: "Xy9!@zAb1#Cd2$Ef"
            var password = CredentialGenerator.GeneratePassword(16);
            
            // Hashear el password usando BCrypt (utilidad de Miguel)
            // Ejemplo resultado: "$2a$10$N9qo8uLOickgx2ZMRZoMye..."
            var passwordHash = PasswordHasher.HashPassword(password);

            _logger.LogInformation($"🔑 Credenciales generadas para usuario: {username}");

            return await Task.FromResult(new CredentialResult
            {
                Username = username,
                Password = password,           // Texto plano - solo para mostrar al usuario UNA vez
                PasswordHash = passwordHash    // Hash BCrypt - esto se guarda en la BD
            });
        }
    }
}