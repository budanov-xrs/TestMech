using System;
using System.Linq;
using System.Text;

namespace Arion.Data.Utilities
{
    public static class Crypt
    {
        public static string Encrypt(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static string Decrypt(string text)
        {
            var bytes = Enumerable.Range(0, text.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(text.Substring(x, 2), 16))
                .ToArray();

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
