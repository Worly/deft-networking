using System;
using System.Text;

namespace Deft.Utils
{
    internal static class Extensions
    {
        public static string ToHexString(this byte[] ba, int size = -1)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);

            int n = size == -1 ? ba.Length : Math.Min(size, ba.Length);

            for(int i = 0; i < n; i++)
                hex.AppendFormat("{0:x2} ", ba[i]);
            return hex.ToString();
        }
    }
}
