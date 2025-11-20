using System.Security.Cryptography;
using System.Text;

namespace CrudCloudDb.Application.Utilities
{
    public static class CredentialGenerator
    {
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NumericChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()-_=+<>?";

        public static string GenerateUsername(int length = 12)
        {
            // Generates a simple, readable username
            const string chars = LowercaseChars + NumericChars;
            return GenerateRandomString(chars, length);
        }

        public static string GeneratePassword(int length = 16)
        {
            // Generates a strong password with all character types
            string allChars = LowercaseChars + UppercaseChars + NumericChars + SpecialChars;
            
            var password = new StringBuilder();
            password.Append(GetRandomChar(LowercaseChars));
            password.Append(GetRandomChar(UppercaseChars));
            password.Append(GetRandomChar(NumericChars));
            password.Append(GetRandomChar(SpecialChars));

            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars));
            }

            // Shuffle the result to avoid predictable patterns
            return new string(password.ToString().ToCharArray().OrderBy(c => RandomNumberGenerator.GetInt32(0, int.MaxValue)).ToArray());
        }

        private static string GenerateRandomString(string charSet, int length)
        {
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = charSet[RandomNumberGenerator.GetInt32(0, charSet.Length)];
            }
            return new string(result);
        }

        private static char GetRandomChar(string charSet)
        {
            return charSet[RandomNumberGenerator.GetInt32(0, charSet.Length)];
        }
        public static string GenerateNumericToken(int length = 6)
        {
            return string.Create(length, length, (span, state) =>
            {
                for (var i = 0; i < state; i++)
                {
                    span[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
                }
            });
        }
    }
    
    
}