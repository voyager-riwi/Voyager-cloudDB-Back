// CrudCloudDb.Application/Utilities/PasswordHasher.cs

using BCrypt.Net;

namespace CrudCloudDb.Application.Utilities
{
    /// <summary>
    /// Utilidad para hashear y verificar passwords usando BCrypt
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Hashea un password en texto plano usando BCrypt
        /// </summary>
        /// <param name="password">Password en texto plano</param>
        /// <returns>Hash BCrypt del password</returns>
        public static string HashPassword(string password)
        {
            // BCrypt.Net-Next handles the salt generation automatically
            // Work factor default: 10 (2^10 = 1024 iteraciones)
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifica si un password en texto plano coincide con un hash BCrypt
        /// </summary>
        /// <param name="password">Password en texto plano</param>
        /// <param name="hashedPassword">Hash BCrypt almacenado</param>
        /// <returns>true si coincide, false si no</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}