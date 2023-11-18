using System;
using System.Security.Cryptography;
using System.Text;

namespace HashMat.Helpers
{
    public class Helper
    {
        public static string GetHashOfFile(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(memoryStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }

        public static string GetReadableTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}
