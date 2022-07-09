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
                return new Vector3(1, 1, 1) - (0.07f * new Vector3(value, value, value));
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * Vector3.Zero;
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

        public static Vector3 GetRayColor(CustomRay ray, Surface world, Random random, int depth, Light light, float clamp, int samples)
        {
            hitRecord record = new hitRecord();

            Vector3 direction = ray.direction;

            // break out if too many rays have already been recursively cast
            if (depth < 1)
                return Vector3.Zero;

            // if object is hit
            if (world.hit(ray, 0.01f, float.PositiveInfinity, record))
            {
                if (record.hitMat.smoothness < 0.6f)
                {
                    depth -= (int)((float)depth / 4);
                    samples += (int)((float)samples / 8);
                }
                if (record.hitMat.smoothness < 0.4f)
                {
                    depth -= (int)((float)depth / 2);
                    samples += (int)((float)samples / 10);
                }
                if (record.hitMat.smoothness < 0.2f)
                {
                    depth -= (int)((float)depth / 2);
                    samples += (int)((float)samples / 12);
                }
                if (record.hitMat.smoothness <= 0.05f)
                {
                    depth = 1;
                    samples += (int)((float)samples / 14);
                }

                if (record.hitMat.smoothness >= 0.8f)
                    samples -= (int)((float)samples / 8);
                if (record.hitMat.smoothness >= 0.9f)
                    samples -= (int)((float)samples / 8);
                if (record.hitMat.smoothness >= 0.95f)
                    samples -= (int)((float)samples / 8);
                
                depth = Math.Clamp(depth, 1, 999999);

                
                Vector3 diffuseDirection = record.normal + RandomVector.ReturnHemisphereRandomVector(random, record.normal);// + RandomVector.ReturnRandomNormalizedUnitSphereVector(random);
                /*
                if (CustomVectorMath.NearZero(diffuseDirection))
                {
                    diffuseDirection = record.normal;
                    Console.Write("Near Zero!");
                }
                */
                
                Vector3 reflectedDirection = Vector3.Reflect(direction, record.normal + RandomVector.ReturnRandomRangedVector(random, (float)Math.Sqrt(record.hitMat.smoothness)));
                
                /*if (CustomVectorMath.NearZero(reflectedDirection))
                {
                    reflectedDirection = record.normal;
                    Console.Write("Near Zero!");
                }
                */
                
                float fresnel = (CustomVectorMath.AngleBetween(direction, reflectedDirection) / 180);
                
                // default fresnel squared
                fresnel *= fresnel;

                Vector3 finalColor = Vector3.Zero;
                // first point light pass
                Vector3 lightColor = GetLights(ray, world, random, light);
                bool doMetalPass = true;
                bool doReflectPass = true;

                if (record.hitMat.metalness == 0)
                    doMetalPass = false;
                if (record.hitMat.metalness == 1)
                    doReflectPass = false;

                // diffuse GI pass
                if (doMetalPass && doReflectPass)
                {
                    finalColor = (record.hitMat.emission + (record.hitMat.albedo * (1 - record.hitMat.metalness))
                        * 0.7f * (GetRayColor(new CustomRay(record.point, diffuseDirection), world, random, depth - 1, light, clamp, samples)))
                        // metalness pass
                        + (record.hitMat.albedo * record.hitMat.metalness)
                        * GetRayColor(new CustomRay(record.point, reflectedDirection), world, random, depth - 1, light, clamp, samples)
                        // additive reflectivity pass w basic fresnel
                        + (1 - record.hitMat.metalness) * (fresnel * (record.hitMat.smoothness * record.hitMat.smoothness))
                        * GetRayColor(new CustomRay(record.point, reflectedDirection), world, random, depth - 1, light, clamp, samples)
                        + lightColor;
                }
                else if (doMetalPass && !doReflectPass)
                {
                    finalColor = (record.hitMat.emission + (record.hitMat.albedo * (1 - record.hitMat.metalness))
                        * 0.7f * (GetRayColor(new CustomRay(record.point, diffuseDirection), world, random, depth - 1, light, clamp, samples)))
                        // metalness pass
                        + (record.hitMat.albedo * record.hitMat.metalness)
                        * GetRayColor(new CustomRay(record.point, reflectedDirection), world, random, depth - 1, light, clamp, samples)
                        + lightColor;
                }
                else if (!doMetalPass && doReflectPass)
                {
                    finalColor = (record.hitMat.emission + (record.hitMat.albedo * (1 - record.hitMat.metalness))
                        * 0.7f * (GetRayColor(new CustomRay(record.point, diffuseDirection), world, random, depth - 1, light, clamp, samples)))
                        // additive reflectivity pass w basic fresnel
                        + (1 - record.hitMat.metalness) * (fresnel * (record.hitMat.smoothness * record.hitMat.smoothness))
                        * GetRayColor(new CustomRay(record.point, reflectedDirection), world, random, depth - 1, light, clamp, samples)
                        + lightColor;
                }
                
                // clamp return value to reduce noise
                finalColor.X = Math.Clamp(finalColor.X, 0, clamp);
                finalColor.Y = Math.Clamp(finalColor.Y, 0, clamp);
                finalColor.Z = Math.Clamp(finalColor.Z, 0, clamp);

                return finalColor;
            }
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(0.2f, 0.17f, 0.17f) + (1 - t) * new Vector3(0.25f, 0.22f, 0.23f);
            //return t * new Vector3(0f,0f,0f);
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