using Microsoft.Xna.Framework;
using System;

namespace PixelManagement
{
    public static class PixelOperations
    {
        public static void WritePixel(Color[] buffer, int x, int y, int width, Color color)
        {
            int tempR = color.R;
            int tempG = color.G;
            int tempB = color.B;
            
            tempR = Math.Clamp(tempR, 0, 255);
            tempG = Math.Clamp(tempG, 0, 255);
            tempB = Math.Clamp(tempB, 0, 255);

            color.R = (byte)tempR;
            color.G = (byte)tempG;
            color.B = (byte)tempB;
            
            buffer[x + (y * width)] = color;
        }
    }
}