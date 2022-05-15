using Microsoft.Xna.Framework;
using System;

namespace MiscFunctions
{   
    // Vector based random functions
    public static class RandomVector
    {
        // return normalize random vector
        public static Vector3 ReturnRandomVector(Random random)
        {
            return Vector3.Normalize(new Vector3((float)random.Next(-100, 100) / 100, (float)random.Next(-100, 100) / 100, (float)random.Next(-100, 100) / 100));
        }

        // for minute variances in reflection (roughness)
        public static Vector3 ReturnRandomRangedVector(Random random, float minimization)
        {            
            return (new Vector3((float)random.Next(-100, 100) / minimization, (float)random.Next(-100, 100) / minimization, (float)random.Next(-100, 100) / minimization));
        }

        // return random vector within unit sphere
        public static Vector3 ReturnRandomUnitSphereVector(Random random)
        {
            while (true)
            {
                Vector3 p = ReturnRandomVector(random);
                if (Vector3.DistanceSquared(p, Vector3.Zero) >=1) continue;
                return p;
            }
        }

        public static Vector3 ReturnRandomNormalizedUnitSphereVector(Random random)
        {
            return Vector3.Normalize(ReturnRandomUnitSphereVector(random));
        }
    }
}