using System.Security.Cryptography;
using System.Text;

namespace HRPermissionManagement.Helpers
{
    public static class Hasher
    {
        public static string HashPassword(string password)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = SHA256.HashData(inputBytes);

            // Convert byte array to string
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}