using Microsoft.Extensions.Logging;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Utilities;

namespace CrudCloudDb.Infrastructure.Services
{
    public class CredentialService : ICredentialService
    {
        private readonly ILogger<CredentialService> _logger;

        public CredentialService(ILogger<CredentialService> logger)
        {
            _logger = logger;
        }
        
        public async Task<CredentialResult> GenerateCredentialsAsync()
        {
            var username = CredentialGenerator.GenerateUsername(12);

            var password = CredentialGenerator.GeneratePassword(16);

            var passwordHash = PasswordHasher.HashPassword(password);

            _logger.LogInformation($" Credenciales generadas para usuario: {username}");

            return await Task.FromResult(new CredentialResult
            {
                Username = username,
                Password = password,           
                PasswordHash = passwordHash  
            });
        }
    }
}