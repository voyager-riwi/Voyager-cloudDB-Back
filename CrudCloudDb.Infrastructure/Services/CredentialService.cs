using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Credential;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CrudCloudDb.Infrastructure.Services
{
    public class CredentialService : ICredentialService
    {
        private readonly ILogger<CredentialService> _logger;

        public CredentialService(ILogger<CredentialService> logger)
        {
            _logger = logger;
        }

        public Task<CredentialsResult> GenerateCredentialsAsync()
        {
            var username = GenerateUsername();
            var password = GenerateSecurePassword();
            var passwordHash = HashPassword(password);

            return Task.FromResult(new CredentialsResult
            {
                Username = username,
                Password = password,
                PasswordHash = passwordHash
            });
        }

        public string GenerateDatabaseName()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            const string alphanumeric = "abcdefghijklmnopqrstuvwxyz0123456789";
            
            var result = new StringBuilder("db_"); // Prefijo "db_"
            
            // Primera letra
            result.Append(letters[RandomNumberGenerator.GetInt32(letters.Length)]);
            
            // Resto: 15 caracteres aleatorios
            for (int i = 0; i < 15; i++)
            {
                result.Append(alphanumeric[RandomNumberGenerator.GetInt32(alphanumeric.Length)]);
            }

            return result.ToString(); // Ejemplo: db_x7k9m2p4v8n1q6w3
        }

        private string GenerateUsername()
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            const string alphanumeric = "abcdefghijklmnopqrstuvwxyz0123456789";
            
            var result = new StringBuilder();
            result.Append(letters[RandomNumberGenerator.GetInt32(letters.Length)]);
            
            for (int i = 0; i < 15; i++)
            {
                result.Append(alphanumeric[RandomNumberGenerator.GetInt32(alphanumeric.Length)]);
            }

            return result.ToString();
        }

        private string GenerateSecurePassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "@#$%^&*-_+=";
            
            var password = new StringBuilder();
            
            password.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            password.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);
            
            const string allChars = uppercase + lowercase + digits + special;
            for (int i = 0; i < 12; i++)
            {
                password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }
            
            return new string(password.ToString()
                .OrderBy(x => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray());
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }
    }
}