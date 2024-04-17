using System.Text;

namespace SummaryCheck.Utilities
{
    public static class StringUtils
    {
        public static string ToHexString(byte[] bytes)
        {
            StringBuilder s = new(bytes.Length * 2);
            foreach (var item in bytes)
            {
                s.Append(item.ToString("X2"));
            }

            return s.ToString();
        }
    }
}
