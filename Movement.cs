using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    struct Movement
    {
        public float dX;
        public float dY;

        public bool IsDefault()
        {
            return (Math.Abs(dX) + Math.Abs(dY)) == 0;
        }

        public void FlipDirection()
        {
            dX *= -1;
            dY *= -1;
        }
    }
}
