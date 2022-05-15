using Microsoft.Xna.Framework;
using System;

namespace MiscFunctions
{   
    public static class RandomNormalVector
    {
        public static Vector3 ReturnRandomVector(Random random)
        {
            return Vector3.Normalize(new Vector3((float)random.Next(0, 100) / 100, (float)random.Next(0, 100) / 100, (float)random.Next(0, 100) / 100));
        }
    }
}