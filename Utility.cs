using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    static class Utility
    {
        public static float GetRandom(float variance)
        {
            if (variance >= 1 || variance <= 0) return 0f;
            var int32buffer = new byte[4];
            Gen.GetNonZeroBytes(int32buffer);
            // ensure positive
            int32buffer[3] &= 0x7f;
            var number = BitConverter.ToInt32(int32buffer);
            // get a random float between -variance and variance
            return (((float)number / (float)Int32.MaxValue) * (2 * variance)) - variance;
        }

        #region private
        private static RandomNumberGenerator Gen;

        static Utility()
        {
            Gen = RandomNumberGenerator.Create();
        }
        #endregion
    }
}
