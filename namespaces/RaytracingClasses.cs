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
        public Camera(Vector3 inputOrigin, Vector3 lookAt, float inFov, float ratio)
        {
            position = inputOrigin;

            float theta = MathHelper.ToRadians(inFov);
            float h = (float)Math.Tan(theta / 2);
            viewportHeight = 2f * h;
            viewportWidth = viewportHeight * ratio;

            Vector3 w = Vector3.Normalize(position - lookAt);
            Vector3 u = Vector3.Normalize(Vector3.Cross(Vector3.Up, w));
            Vector3 v = Vector3.Cross(w, u);

            horizontal = viewportWidth * u;
            vertical = viewportHeight * v;
            lowerLeftCorner = position - horizontal/2 - vertical/2 - w;
        }

        public CustomRay returnRay(float u, float v)
        {
            return new CustomRay(position, (lowerLeftCorner + u * horizontal + v * vertical) - position);
        }

        public Vector3 position;
        public float vFov;
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
            lightColor = Vector3.Zero;
        }

        public PointLight(Vector3 inputOrigin, float inputBrightness, Vector3 inputColor)
        {
            position = inputOrigin;
            brightness = inputBrightness;
            lightColor = inputColor;

            float biggestColor1 = Math.Max(lightColor.X, lightColor.Y);
            float biggestColor2 = Math.Max(lightColor.Y, lightColor.Z);
            float biggestColor3 = Math.Max(biggestColor1, biggestColor2);
            if (biggestColor3 != 0 && biggestColor3 >= 1) brightness /= biggestColor3;
        }

        public override Vector3 calculateLight(Vector3 formerLightLevel, CustomRay ray, Surface world, hitRecord record, Random random, bool blinnPhong)
        {
            // get roughness for specular calculation
            float roughness = 1 - record.hitMat.smoothness;
            float roughSqr = roughness * roughness;
            float geoShadow = 1;
            float attenuation = Math.Clamp(1 / Vector3.DistanceSquared(record.point, position), 0, 1);
            
            // direction to light
            Vector3 directionToLight = Vector3.Normalize((position - record.point));

            Vector3 halfAngle = Vector3.Normalize(directionToLight + Vector3.Normalize(-ray.direction));
            
            float VdotH = Math.Clamp(Vector3.Dot(-ray.direction, halfAngle), 0, 1);
            float NdotL = Math.Clamp(Vector3.Dot(record.normal, directionToLight), 0, 1);
            float NdotV = Math.Clamp(Vector3.Dot(record.normal, Vector3.Normalize(-ray.direction)), 0, 1);
            float LdotH = Math.Clamp(Vector3.Dot(directionToLight, halfAngle), 0, 1);
            float NdotH = Math.Clamp(Vector3.Dot(record.normal, halfAngle), 0, 1);
            float NdotHSqr = NdotH * NdotH;
            float tanNdotHSqr = (1 - NdotHSqr) / NdotHSqr;

            float ggx = 0;
            float blinn = 0;

            if (!blinnPhong)
            {
                float NdotLSqr = NdotL * NdotL;
                float NdotVSqr = NdotV * NdotV;
                float SmithL = 2 / (1 + (float)Math.Sqrt(1 + roughSqr * (1 - NdotLSqr) / NdotLSqr));
                float SmithV = 2 / (1 + (float)Math.Sqrt(1 + roughSqr * (1 - NdotVSqr) / NdotVSqr));
                geoShadow = SmithL * SmithV;
                
                float F, D, vis;

                // D
                float aSqr = roughSqr;// * roughSqr;
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
                ggx *= (brightness / 2);
                //ggx *= (record.hitMat.smoothness * record.hitMat.smoothness);
            }
            else
            {
                float NdotLSqr = NdotL * NdotL;
                float NdotVSqr = NdotV * NdotV;
                float calcL = (NdotL)/(roughSqr * (float)Math.Sqrt(1 - NdotLSqr));
                float calcV = (NdotV)/(roughSqr * (float)Math.Sqrt(1 - NdotVSqr));
                float SmithL = calcL < 1.6 ? (((3.535f * calcL) + (2.181f * calcL * calcL)) / (1 + (2.276f * calcL) + (2.577f * calcL * calcL))) : 1;
                float SmithV = calcV < 1.6 ? (((3.535f * calcV) + (2.181f * calcV * calcV)) / (1 + (2.276f * calcV) + (2.577f * calcV * calcV))) : 1;
                geoShadow = SmithL * SmithV;
                
                blinn = Vector3.Dot(record.normal, halfAngle);
                blinn = Math.Clamp(blinn, 0, 1);
                blinn = NdotL != 0 ? blinn : 0;
                blinn = (float)Math.Pow(blinn, ((record.hitMat.smoothness * record.hitMat.smoothness) * 200));
                blinn *= (record.hitMat.smoothness * record.hitMat.smoothness) + 0.001f;
                blinn *= brightness;
            }

            Vector3 lightAlbedo;
            lightAlbedo = (record.hitMat.albedo + lightColor);

            Vector3 lightLevel = formerLightLevel + ((lightAlbedo * (1 - record.hitMat.metalness)) * (NdotL*NdotV) * geoShadow * brightness * attenuation);

            // point light pass
            if (world.hit(new CustomRay(record.point, directionToLight + RandomVector.ReturnRandomRangedVector(random, 0.975f)), 0.001f, float.PositiveInfinity, record))
                if (CustomVectorMath.Magnitude(new CustomRay(record.point, directionToLight).getPos(record.t) - record.point) < CustomVectorMath.Magnitude(position - record.point))
                {
                    lightLevel = formerLightLevel;
                    if (!blinnPhong)
                        ggx = 0;
                    else
                        blinn = 0;
                }

            if (!blinnPhong)
                return lightLevel + ((Vector3.Lerp(new Vector3(1,1,1) + lightColor, lightAlbedo, record.hitMat.metalness) / (float)Math.PI) * ggx * attenuation);
            else 
                return lightLevel + (Vector3.Lerp(new Vector3(1,1,1) + lightColor, lightAlbedo, record.hitMat.metalness) * blinn * attenuation);
        }

        public Vector3 position;
        public float brightness;
        public Vector3 lightColor;
    }

    public class LightList : Light
    {
        public List<Light> lights = new List<Light>();

        public override Vector3 calculateLight(Vector3 formerLightLevel, CustomRay ray, Surface world, hitRecord record, Random random, bool blinnPhong)
        {
            foreach (Light light in lights)
            {
                if (world.hit(ray, 0.001f, float.PositiveInfinity, record))
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
            bool hitAnything = false;
            float closestSoFar = t_max;

            foreach (Surface surface in surfaces)
            {
                if (surface.hit(ray, t_min, closestSoFar, record))
                {
                    hitAnything = true;
                    closestSoFar = record.t;
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
    
    // axis aligned rectangle
    public class RectAxis : Surface
    {
        public RectAxis(float inX1, float inX2, float inY1, float inY2, float inK, Material inputMat)
        {
            x1 = inX1;
            x2 = inX2;
            y1 = inY1;
            y2 = inY2;
            k = inK;
            material = inputMat;
        }

        public override bool hit(CustomRay ray, float t_min, float t_max, hitRecord record)
        {
            float t = (-k - ray.origin.Z / ray.direction.Z);
            if (t < t_min || t > t_max)
                return false;
            
            float x = ray.origin.X + (t * ray.direction.X);
            float y = ray.origin.Y + (t * ray.direction.Y);
            if (x < x1 || x > x2 || y < y1 || y > y2)
                return false;
            
            if (t < 0)
                return false;
            
            record.t = t;
            Vector3 outwardNormal = new Vector3(0, 0, 1);
            record.setFaceNormal(ray, outwardNormal);
            record.hitMat = material;
            record.point = ray.getPos(t);
            return true;
        }

        float x1;
        float x2;
        float y1;
        float y2;
        float k;
        Material material;
    }

    // basic triangle intersection utilizing the mt algorithm
    public class Tri : Surface
    {
        public Tri(Vert v0, Vert v1, Vert v2, Material inputMat, bool inFlip, bool blendNormals)
        {
            vertA = v0.position;
            vertB = v1.position;
            vertC = v2.position;
            vANorm = v0.normal;
            vBNorm = v1.normal;
            vCNorm = v2.normal;
            flipNormal = inFlip;
            blend = blendNormals;
            material = inputMat;
        }

        public Tri(Vert v0, Vert v1, Vert v2, Material inputMat, bool blendNormals)
        {
            vertA = v0.position;
            vertB = v1.position;
            vertC = v2.position;
            vANorm = v0.normal;
            vBNorm = v1.normal;
            vCNorm = v2.normal;
            flipNormal = false;
            blend = blendNormals;
            material = inputMat;
        }

        public Tri(Vert v0, Vert v1, Vert v2, Material inputMat)
        {
            vertA = v0.position;
            vertB = v1.position;
            vertC = v2.position;
            vANorm = v0.normal;
            vBNorm = v1.normal;
            vCNorm = v2.normal;
            flipNormal = false;
            blend = true;
            material = inputMat;
        }
        
        public override bool hit(CustomRay ray, float t_min, float t_max, hitRecord record)
        {
            Vector3 AB = vertB - vertA;
            Vector3 AC = vertC - vertA;
            Vector3 pVec = Vector3.Cross(ray.direction, AC);
            float discriminant = Vector3.Dot(AB, pVec);

            if (discriminant < 1e-16 && !flipNormal)
                return false;
            else if (discriminant > 1e-16 && flipNormal)
                return false;
            
            if (Math.Abs(discriminant) < 1e-16)
                return false;
            
            float invDisc = 1 / discriminant;

            Vector3 tVec = ray.origin - vertA;
            float u = Vector3.Dot(tVec, pVec) * invDisc;
            if (u < 0 || u > 1)
                return false;
            
            Vector3 qVec = Vector3.Cross(tVec, AB);
            float v = Vector3.Dot(ray.direction, qVec) * invDisc;
            if (v < 0 || u + v > 1)
                return false;
            
            float t = Vector3.Dot(AC, qVec) * invDisc;
            if (t < 0)
                return false;
            if (t < t_min || t > t_max)
                return false;

            record.t = t;
            if (!blend)
            {
                if (!flipNormal)
                    record.normal = Vector3.Normalize(Vector3.Cross(AB, AC));
                else
                    record.normal = -Vector3.Normalize(Vector3.Cross(AB, AC));
            }
            else
            {
                if (!flipNormal)
                    record.normal = Vector3.Barycentric(vANorm, vBNorm, vCNorm, u, v);
                else
                    record.normal = -Vector3.Barycentric(vANorm, vBNorm, vCNorm, u, v);
            }
            record.frontFace = true;
            record.hitMat = material;
            record.point = ray.getPos(t);
            return true;
        }
        
        Vector3 vertA;
        Vector3 vertB;
        Vector3 vertC;
        Vector3 vANorm;
        Vector3 vBNorm;
        Vector3 vCNorm;
        bool flipNormal;
        bool blend;
        Material material;
    }

    public class Vert
    {
        public Vert(Vector3 inPos, Vector3 inNorm)
        {
            position = inPos;
            normal = inNorm;
        }
        
        public Vector3 position;
        public Vector3 normal;
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