// CrudCloudDb.Infrastructure/Services/CredentialService.cs
using System.Security.Cryptography;
using CrudCloudDb.Application.Interfaces.Utilities;
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
            var username = $"user_{GenerateRandomString(8)}";
            var password = GenerateSecurePassword(16);
            var passwordHash = PasswordHasher.HashPassword(password);

            _logger.LogInformation($"🔑 Credenciales generadas para usuario: {username}");

            return await Task.FromResult(new CredentialResult
            {
                Username = username,
                Password = password,
                PasswordHash = passwordHash
            });
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
        }

        private string GenerateSecurePassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
        }
    }

    public class CredentialResult
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}