using Microsoft.Xna.Framework;
using System;
using MiscFunctions;

namespace Raytracing
{
    // used to pull data from the custom ray class
    public static class RayOperations
    {
        // paint normals onto objects in world hit by rays
        public static Vector3 GetRayNormalColor(CustomRay ray, Surface world)
        {
            hitRecord record = new hitRecord();
            if (world.hit(ray, 0, float.PositiveInfinity, record))
            {
                return 0.5f * (record.normal + new Vector3(1, 1, 1));
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(0.2f, 0.2f, 0.2f) + (1 - t) * new Vector3(0.5f, 0.7f, 1);
        }

        // get depth buffer
        public static Vector3 GetRayDepthColor(CustomRay ray, Surface world)
        {
            hitRecord record = new hitRecord();
            if (world.hit(ray, 0, float.PositiveInfinity, record))
            {
                float value = Vector3.Distance(ray.origin, record.point);
                return new Vector3(1, 1, 1) - (0.05f * new Vector3(value, value, value));
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(0.2f, 0.2f, 0.2f) + (1 - t) * new Vector3(0.5f, 0.7f, 1);
        }

        // get normal world render
        public static Vector3 GetRayColor(CustomRay ray, Surface world, Random random, int depth)
        {
            hitRecord record = new hitRecord();

            // break out if too many rays have already been recursively cast
            if (depth < 1)
                return Vector3.Zero;

            if (world.hit(ray, 0.001f, float.PositiveInfinity, record))
            {
                Vector3 target = record.point + record.normal + RandomVector.ReturnRandomNormalizedUnitSphereVector(random);
                return 0.5f * (GetRayColor(new CustomRay(record.point, target - record.point), world, random, depth-1));
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(0.85f, 0.75f, 1f) + (1 - t) * new Vector3(1, 1, 1);
        }

        public static float RaySphereIntersection(Sphere sphere, CustomRay ray)
        {
            Vector3 distance = ray.origin - sphere.origin;
            float a = Vector3.DistanceSquared(ray.direction, Vector3.Zero);
            float half_b = Vector3.Dot(distance, ray.direction);
            float c = Vector3.DistanceSquared(distance, Vector3.Zero) - sphere.radius * sphere.radius;
            float discriminant = half_b*half_b - a*c;

            if (discriminant < 0)
                return -1;
            else
                return (-half_b - (float)Math.Sqrt(discriminant)) / (2*a);
        }
    }
}