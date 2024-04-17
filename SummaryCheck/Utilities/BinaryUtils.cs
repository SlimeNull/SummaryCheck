using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummaryCheck.Utilities
{
    public class BinaryUtils
    {
        public static async Task XorBytesAsync(byte[] bytes, long count, byte value)
        {
            if (count > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count > (500 << 10))
            {
                await Task.Run(() =>
                {
                    for (long i = 0; i < count; i++)
                        bytes[i] ^= value;
                });
            }
            else
            {
                for (long i = 0; i < count; i++)
                    bytes[i] ^= value;
            }
        }
    }
}
