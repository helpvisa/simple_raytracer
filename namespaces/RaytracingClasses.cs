using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using MiscFunctions;

namespace Raytracing
{
    // class used to define the rays being cast
    public class CustomRay
    {
        public CustomRay(Vector3 inputOrigin, Vector3 inputDirection)
        {
            origin = inputOrigin;
            direction = Vector3.Normalize(inputDirection);
        }
        
        public Vector3 origin;
        public Vector3 direction;
        
        public Vector3 getPos(float t)
        {
            return origin + (t * direction);
        }
    }

    // class used to define the camera object
    public class Camera
    {
        public Camera(Vector3 inputOrigin, Vector3 inputDirection, float inputFocal, float height, float ratio)
        {
            position = inputOrigin;
            direction = Vector3.Normalize(inputDirection);
            focalLength = inputFocal;
            viewportHeight = height;
            viewportWidth = viewportHeight * ratio;
            horizontal = new Vector3(viewportWidth, 0, 0);
            vertical = new Vector3(0, viewportHeight, 0);
            lowerLeftCorner = position - horizontal/2 - vertical/2 - direction * focalLength;
        }

        public Vector3 position;
        public Vector3 direction;
        public float focalLength;
        float viewportHeight;
        float viewportWidth;
        public Vector3 horizontal;
        public Vector3 vertical;
        public Vector3 lowerLeftCorner;
    }

    // general light class from which all types are derived
    public class Light
    {
        public virtual Vector3 calculateLight(Vector3 formerLightLevel, CustomRay ray, Surface world, hitRecord record, Random random, bool blinnPhong)
        {
            return Vector3.Zero;
        }
    }

    public class PointLight : Light
    {
        public PointLight(Vector3 inputOrigin, float inputBrightness)
        {
            position = inputOrigin;
            brightness = inputBrightness;
        }

        public override Vector3 calculateLight(Vector3 formerLightLevel, CustomRay ray, Surface world, hitRecord record, Random random, bool blinnPhong)
        {
            // get roughness for specular calculation
            float roughness = 1 - record.hitMat.smoothness;
            float roughSqr = roughness * roughness;
            
            // direction to light
            Vector3 directionToLight = Vector3.Normalize((position - record.point) + (RandomVector.ReturnRandomRangedVector(random, 0.9f)));

            float attenuation = Vector3.DistanceSquared(position, record.point) / 100;

            Vector3 halfAngle = Vector3.Normalize(directionToLight + Vector3.Normalize(-ray.direction));
            
            float NdotL = /*Math.Clamp(*/Vector3.Dot(record.normal, directionToLight);//, 0, 1);
            float NdotV = /*Math.Clamp(*/Vector3.Dot(record.normal, Vector3.Normalize(-ray.direction));//, 0, 1);
            float LdotH = /*Math.Clamp(*/Vector3.Dot(directionToLight, halfAngle);//, 0, 1);
            float NdotH = /*Math.Clamp(*/Vector3.Dot(record.normal, halfAngle);//, 0, 1);
            float NdotHSqr = NdotH * NdotH;
            float tanNdotHSqr = (1 - NdotHSqr) / NdotHSqr;

            float ggx = 0;
            float blinn = 0;

            if (!blinnPhong)
            {
                float F, D, vis;

                // D
                float aSqr = roughSqr * roughSqr;
                float pi = (float)Math.PI;
                float denom = NdotH * NdotH * (aSqr - 1f) + 1f;
                D = aSqr / (pi * denom * denom);

                // F
                float LdotH5 = (float)Math.Pow(1f - LdotH, 5);
                float f0 = 0.5f;
                F = f0 + (1f - f0) * LdotH5;

                // V
                float k = roughSqr / 2f;
                vis = (1f / (NdotL * (1f - k) + k)) * (1f / (NdotV * (1f - k) + k));

                ggx = NdotL * D * F * vis;
                ggx *= brightness;
                ggx *= (record.hitMat.smoothness * record.hitMat.smoothness);
            }
            else
            {
                blinn = Vector3.Dot(record.normal, halfAngle);
                blinn = Math.Clamp(blinn, 0, 1);
                blinn = NdotL != 0 ? blinn : 0;
                blinn = (float)Math.Pow(blinn, ((record.hitMat.smoothness * record.hitMat.smoothness) * 200));
                blinn *= (record.hitMat.smoothness * record.hitMat.smoothness) + 0.001f;
                blinn *= brightness * 1.5f;
            }

            Vector3 lightLevel = formerLightLevel + ((record.hitMat.albedo * (1 - record.hitMat.metalness) * brightness) * NdotL * attenuation);

            // point light pass
            if (world.hit(new CustomRay(record.point, directionToLight), 0.001f, float.PositiveInfinity, record))
                if (CustomVectorMath.Magnitude(new CustomRay(record.point, directionToLight).getPos(record.t) - record.point) < CustomVectorMath.Magnitude(position - record.point))
                {
                    lightLevel = formerLightLevel;
                    if (!blinnPhong)
                        ggx = 0;
                    else
                        blinn = 0;
                }

            if (!blinnPhong)
                return lightLevel + ((Vector3.Lerp(new Vector3(1, 1, 1), record.hitMat.albedo, record.hitMat.metalness) / (float)Math.PI) * ggx * attenuation);
            else 
                return lightLevel + (Vector3.Lerp(new Vector3(1, 1, 1), record.hitMat.albedo, record.hitMat.metalness) * blinn * attenuation);
        }

        public Vector3 position;
        public float brightness;
    }

    public class LightList : Light
    {
        public List<Light> lights = new List<Light>();

        public override Vector3 calculateLight(Vector3 formerLightLevel, CustomRay ray, Surface world, hitRecord record, Random random, bool blinnPhong)
        {
            foreach (Light light in lights)
            {
                if (world.hit(ray, 0.001f, 150f, record))
                {
                    formerLightLevel += light.calculateLight(formerLightLevel, ray, world, record, random, blinnPhong);
                }
            }
            return formerLightLevel;
        }
    }

    public class Material
    {
        public Material(Vector3 inputAlbedo, float inputMetal, float inputSmooth, Vector3 inputEmission, int inMaxDepth)
        {
            inputAlbedo.X = Math.Clamp(inputAlbedo.X, 0, 1);
            inputAlbedo.Y = Math.Clamp(inputAlbedo.Y, 0, 1);
            inputAlbedo.Z = Math.Clamp(inputAlbedo.Z, 0, 1);

            inputMetal = Math.Clamp(inputMetal, 0, 1);

            inputSmooth = Math.Clamp(inputSmooth, 0, 1);
            
            albedo = inputAlbedo;
            metalness = inputMetal;
            smoothness = inputSmooth;
            emission = inputEmission;
            maxDepth = inMaxDepth;
        }

        public Material(Vector3 inputAlbedo, float inputMetal, float inputSmooth, Vector3 inputEmission)
        {
            inputAlbedo.X = Math.Clamp(inputAlbedo.X, 0, 1);
            inputAlbedo.Y = Math.Clamp(inputAlbedo.Y, 0, 1);
            inputAlbedo.Z = Math.Clamp(inputAlbedo.Z, 0, 1);

            inputMetal = Math.Clamp(inputMetal, 0, 1);

            inputSmooth = Math.Clamp(inputSmooth, 0, 1);
            
            albedo = inputAlbedo;
            metalness = inputMetal;
            smoothness = inputSmooth;
            emission = inputEmission;
            maxDepth = 0;
        }

        public Vector3 albedo;
        public float metalness;
        public float smoothness;
        public Vector3 emission;
        public int maxDepth;
    }

    // general class defining hittable surfaces
    public class Surface
    {
        public virtual bool hit(CustomRay ray, float t_min, float t_max, hitRecord record)
        {
            return false;
        }
    }

    // stores list of things hit
    public class SurfaceList : Surface
    {
        public List<Surface> surfaces = new List<Surface>();

        public override bool hit(CustomRay ray, float t_min, float t_max, hitRecord record)
        {
            hitRecord tempRecord = record;
            bool hitAnything = false;
            float closestSoFar = t_max;

            foreach (Surface surface in surfaces)
            {
                if (surface.hit(ray, t_min, closestSoFar, tempRecord))
                {
                    hitAnything = true;
                    closestSoFar = tempRecord.t;
                    record = tempRecord;
                }
            }

            return hitAnything;
        }
    }

    // sphere surface derivative
    public class Sphere : Surface
    {
        public Sphere(Vector3 inputOrigin, float inputRadius, Material inputMat)
        {
            origin = inputOrigin;
            radius = inputRadius;
            material = inputMat;
        }

        public override bool hit(CustomRay ray, float t_min, float t_max, hitRecord record)
        {
            Vector3 distance = ray.origin - origin;
            float a = Vector3.DistanceSquared(ray.direction, Vector3.Zero);
            float half_b = Vector3.Dot(distance, ray.direction);
            float c = Vector3.DistanceSquared(distance, Vector3.Zero) - radius * radius;
            float discriminant = half_b*half_b - a*c;

            if (discriminant < 0)
                return false;
            float sqrtDisc = (float)Math.Sqrt(discriminant);

            // find nearest root that lies within acceptable range
            float root = (-half_b - sqrtDisc) / a;
            if (root < t_min || t_max < root)
            {
                root = (-half_b + sqrtDisc) / a;
                if (root < t_min || t_max <root)
                    return false;
            }

            record.t = root;
            record.point = ray.getPos(record.t);
            Vector3 outwardNormal = Vector3.Normalize((record.point - origin) / radius);
            record.setFaceNormal(ray, outwardNormal);
            record.hitMat = material;

            return true;
        }

        public Vector3 origin;
        public float radius;
        public Material material;
    }

    // data class for storing data about hit points
    public class hitRecord
    {
        public Vector3 point;
        public Vector3 normal;
        public float t;
        public bool frontFace;
        public Material hitMat;

        public void setFaceNormal(CustomRay ray, Vector3 outwardNormal)
        {
            frontFace = Vector3.Dot(ray.direction, outwardNormal) < 0;
            normal = frontFace ? outwardNormal : -outwardNormal;
        }
    }
}