using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

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

    // general class defining hittable surfaces
    public class Surface
    {
        public Vector3 point;
        public Vector3 normal;
        public float t;
        public bool frontFace;

        public void setFaceNormal(CustomRay ray, Vector3 outwardNormal)
        {
            frontFace = Vector3.Dot(ray.direction, outwardNormal) < 0;
            normal = frontFace ? outwardNormal : -outwardNormal;
        }
        
        public virtual bool hit(CustomRay ray, float t_min, float t_max)
        {
            return false;
        }
    }

    public class Sphere : Surface
    {
        public Sphere(Vector3 inputOrigin, float inputRadius)
        {
            origin = inputOrigin;
            radius = inputRadius;
        }

        public override bool hit(CustomRay ray, float t_min, float t_max)
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

            t = root;
            point = ray.getPos(t);
            Vector3 outwardNormal = (point - origin) / radius;
            setFaceNormal(ray, outwardNormal);

            return true;
        }

        public Vector3 origin;
        public float radius;
    }

    // stores list of things hit
    public class SurfaceList : Surface
    {
        public List<Surface> surfaces = new List<Surface>();

        public override bool hit(CustomRay ray, float t_min, float t_max)
        {
            bool hitAnything = false;
            float closestSoFar = t_max;

            foreach (Surface surface in surfaces)
            {
                if (surface.hit(ray, t_min, t_max))
                {
                    hitAnything = true;
                    closestSoFar = t;
                }
            }

            return hitAnything;
        }
    }

    // used to pull data from the custom ray class
    public static class RayFunctions
    {
        public static Vector3 GetRayColor(CustomRay ray, Surface world)
        {
            if (world.hit(ray, 0, float.PositiveInfinity))
            {
                return world.normal;
            }
            Vector3 direction = Vector3.Normalize(ray.direction);
            float t = 0.5f * (direction.Y + 1);
            return t * new Vector3(1, 1, 1) + (1 - t) * new Vector3(0.5f, 0.7f, 1);
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