using System;
using System.Security.Cryptography;
using System.Text;

namespace Flippit.Editor
{

    public static class AESHelper
    {
        public static byte[] GenerateKey(string password, byte[] salt)
        {
            const int iterations = 10000;
            const int keySize = 256;

            using Rfc2898DeriveBytes deriveBytes = new(password, salt, iterations);
            return deriveBytes.GetBytes(keySize / 8);
        }

        public static byte[] GenerateIV()
        {
            using Aes aes = Aes.Create();
            aes.GenerateIV();
            return aes.IV;
        }
    }
}
