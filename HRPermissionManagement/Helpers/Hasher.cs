using System.Security.Cryptography;
using System.Text;

namespace HRPermissionManagement.Helpers
{
    public static class Hasher
    {
        private const int KeySize = 32;
        private const int Iterations = 100000;
        private static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
        private const char Delimiter = ':';

        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(KeySize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                _hashAlgorithmName,
                KeySize);

            return string.Join(Delimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool VerifyPassword(string password, string passwordHash)
        {
            var parts = passwordHash.Split(Delimiter);
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            var inputHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                _hashAlgorithmName,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }
    }
}