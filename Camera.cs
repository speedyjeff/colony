using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colony
{
    class Camera : Player
    {
        public Camera()
        {
            Width = 1;
            Height = 1;
            IsSolid = false;
            ShowDefaultDrawing = false;
        }
    }
}
