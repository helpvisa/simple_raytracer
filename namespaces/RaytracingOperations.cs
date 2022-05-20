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

        // get diffuse world render
        public static Vector3 GetRayColorDiffuse(CustomRay ray, Surface world, Random random, int depth)
        {
            hitRecord record = new hitRecord();

            // break out if too many rays have already been recursively cast
            if (depth < 1)
                return Vector3.Zero;

            if (world.hit(ray, 0.001f, float.PositiveInfinity, record))
            {
                Vector3 target = record.point + record.normal + RandomVector.ReturnRandomNormalizedUnitSphereVector(random);
                return 0.5f * (GetRayColorDiffuse(new CustomRay(record.point, target - record.point), world, random, depth-1));
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(0.85f, 0.75f, 1f) + (1 - t) * new Vector3(1, 1, 1);
        }

        // get reflective world render
        public static Vector3 GetRayColorReflection(CustomRay ray, Surface world, Random random, int depth)
        {
            hitRecord record = new hitRecord();

            // break out if too many rays have already been recursively cast
            if (depth < 1)
                return Vector3.Zero;

            if (world.hit(ray, 0.001f, float.PositiveInfinity, record))
            {
                Vector3 target = record.point + Vector3.Reflect(ray.direction, record.normal);
                return 0.5f * (GetRayColorReflection(new CustomRay(record.point, target - record.point), world, random, depth-1));
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(0.85f, 0.75f, 1f) + (1 - t) * new Vector3(1, 1, 1);
        }

        public static Vector3 GetRayColor(CustomRay ray, Surface world, Random random, int depth, Light light)
        {
            hitRecord record = new hitRecord();
            Vector3 direction = Vector3.Normalize(ray.direction);

            // break out if too many rays have already been recursively cast
            if (depth < 1)
                return Vector3.Zero;

            // if object is hit
            if (world.hit(ray, 0.01f, float.PositiveInfinity, record))
            {
                if (depth < record.hitMat.maxDepth)
                {
                    return Vector3.Zero;
                }
                
                // get light + shadow pass
                //Vector3 lightAlbedo = GetLights(ray, world, random, light);
                
                Vector3 diffuseDirection = record.normal + RandomVector.ReturnHemisphereRandomVector(random, record.normal);// + RandomVector.ReturnRandomNormalizedUnitSphereVector(random);
                if (CustomVectorMath.NearZero(diffuseDirection))
                {
                    diffuseDirection = record.normal;
                    Console.Write("Near Zero!");
                }
                
                Vector3 reflectedDirection = Vector3.Reflect(direction, record.normal + RandomVector.ReturnRandomRangedVector(random, (float)Math.Sqrt(record.hitMat.smoothness)));
                if (CustomVectorMath.NearZero(reflectedDirection))
                {
                    reflectedDirection = record.normal;
                    Console.Write("Near Zero!");
                }
                
                float fresnel = (CustomVectorMath.AngleBetween(direction, reflectedDirection) / 180);
                
                // default fresnel squared
                fresnel *= fresnel;

                Vector3 finalColor = Vector3.Zero;

                // diffuse GI pass
                finalColor = (record.hitMat.emission + (record.hitMat.albedo * (1 - record.hitMat.metalness))
                    * 0.5f * (GetRayColor(new CustomRay(record.point + record.normal * 0.01f, diffuseDirection), world, random, depth - 1, light)))
                    // metalness pass
                    + (record.hitMat.albedo * record.hitMat.metalness)
                    * GetRayColor(new CustomRay(record.point + record.normal * 0.01f, reflectedDirection), world, random, depth - 1, light)
                    // additive reflectivity pass w basic fresnel
                    + (1 - record.hitMat.metalness) * (fresnel * (record.hitMat.smoothness * record.hitMat.smoothness))
                    * GetRayColor(new CustomRay(record.point + record.normal * 0.01f, reflectedDirection), world, random, depth - 1, light)
                    // point light pass
                    + GetLights(ray, world, random, light);

                
                // clamp return value to reduce noise
                float clampValue = 1.2f;
                finalColor.X = Math.Clamp(finalColor.X, 0, clampValue);
                finalColor.Y = Math.Clamp(finalColor.Y, 0, clampValue);
                finalColor.Z = Math.Clamp(finalColor.Z, 0, clampValue);

                return finalColor;
            }
            float t = 0.5f * (direction.Y + 1);
            //return t * new Vector3(0.08f, 0.08f, 0.2f) + (1 - t) * new Vector3(0.025f, 0.025f, 0.1f);
            return t * new Vector3(0f,0f,0f);
        }

        public static Vector3 GetLights(CustomRay ray, Surface world, Random random, Light light)
        {
            hitRecord record = new hitRecord();
            Vector3 formerLightLevel = Vector3.Zero;

            formerLightLevel = light.calculateLight(formerLightLevel, ray, world, record, random, false);
            
            return formerLightLevel;
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