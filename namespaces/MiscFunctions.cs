using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Raytracing;

namespace MiscFunctions
{   
    // Vector based random functions
    public static class RandomVector
    {
        // return normalize random vector
        public static Vector3 ReturnRandomVector(Random random)
        {
            return new Vector3((float)random.Next(-100, 100) / 100, (float)random.Next(-100, 100) / 100, (float)random.Next(-100, 100) / 100);
        }

        // for minute variances in reflection (roughness)
        public static Vector3 ReturnRandomRangedVector(Random random, float multiplier)
        {            
            float factor = 1 - Math.Clamp(multiplier, 0, 1);
            
            return (new Vector3(((float)random.Next(-100, 100) / 100) * factor, ((float)random.Next(-100, 100) / 100) * factor, ((float)random.Next(-100, 100) / 100) * factor));
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

        public static Vector3 ReturnHemisphereRandomVector(Random random, Vector3 normal)
        {
            Vector3 unitSphere = ReturnRandomUnitSphereVector(random);
            if (Vector3.Dot(unitSphere, normal) > 0)
                return unitSphere;
            else
                return -unitSphere;
        }
    }

    public static class CustomVectorMath
    {
        public static float AngleBetween(Vector3 a, Vector3 b)
        {
            float dot = Vector3.Dot(a, b);
            Vector3 cross = Vector3.Cross(a, b);

            float angle = (float)(Math.PI - Math.Atan2(cross.Length(), dot));
            return MathHelper.ToDegrees(angle);
        }

        public static bool NearZero(Vector3 a)
        {
            double s = 1e-8;
            return (Math.Abs(a.X) < s) && (Math.Abs(a.Y) < s) && (Math.Abs(a.Z) < s);
        }

        public static float Magnitude(Vector3 a)
        {
            return (a.X*a.X+a.Y*a.Y+a.Z*a.Z);
        }
    }

    public static class BoundingMath
    {
        public static AABB surroundingBox(AABB box0, AABB box1)
        {
            Vector3 small = new Vector3(Math.Min(box0.min.X, box1.min.X),
                                        Math.Min(box0.min.Y, box1.min.Y),
                                        Math.Min(box0.min.Z, box1.min.Z));
            Vector3 big = new Vector3(Math.Max(box0.max.X, box1.max.X),
                                      Math.Max(box0.max.Y, box1.max.Y),
                                      Math.Max(box0.max.Z, box1.max.Z));
            
            return new AABB(big, small);
        }

        public static bool boxCompare(Surface a, Surface b, int axis)
        {
            //AABB box_a = a.bounds;
            //AABB box_b = b.bounds;
            Vector3 aCent = a.bounds.min + (a.bounds.min - a.bounds.max / 2);
            Vector3 bCent = b.bounds.min + (b.bounds.min - b.bounds.max / 2);
            
            switch (axis)
            {
                case 0:
                    return aCent.X < bCent.X;
                case 1:
                    return aCent.Y < bCent.Y;
                case 2:
                    return aCent.Z < bCent.Z;
                default:
                    return false;
            }
            /*
            switch (axis)
            {
                case 0:
                    return box_a.min.X < box_b.min.X;
                case 1:
                    return box_a.min.Y < box_b.min.Y;
                case 2:
                    return box_a.min.Z < box_b.min.Z;
                default:
                    return false;
            }
            */
        }
    }

    public static class Sorting
    {
        public static Color[] BlurArray(Color[] array, int width, int height, float weight)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y > 0 && y < height - 1 && x > 0 && x < width - 1)
                    {
                        Vector3 originalColor;
                        Vector3 tempColorLeft;
                        Vector3 tempColorRight;
                        Vector3 tempColorUp;
                        Vector3 tempColorDown;
                        Vector3 tempColorLeftUp;
                        Vector3 tempColorRightUp;
                        Vector3 tempColorLeftDown;
                        Vector3 tempColorRightDown;

                        weight = Math.Clamp(weight, 0, 1);

                        originalColor.X = array[x + (y * width)].R;
                        originalColor.Y = array[x + (y * width)].G;
                        originalColor.Z = array[x + (y * width)].B;

                        tempColorLeft.X = array[x - 1 + (y * width)].R;
                        tempColorLeft.Y = array[x - 1 + (y * width)].G;
                        tempColorLeft.Z = array[x - 1 + (y * width)].B;

                        tempColorRight.X = array[x + 1 + (y * width)].R;
                        tempColorRight.Y = array[x + 1 + (y * width)].G;
                        tempColorRight.Z = array[x + 1 + (y * width)].B;

                        tempColorUp.X = array[x + ((y - 1) * width)].R;
                        tempColorUp.Y = array[x + ((y - 1) * width)].G;
                        tempColorUp.Z = array[x + ((y - 1) * width)].B;

                        tempColorDown.X = array[x + ((y + 1) * width)].R;
                        tempColorDown.Y = array[x + ((y + 1) * width)].G;
                        tempColorDown.Z = array[x + ((y + 1) * width)].B;

                        tempColorLeftUp.X = array[x - 1 + ((y - 1) * width)].R;
                        tempColorLeftUp.Y = array[x - 1 + ((y - 1) * width)].G;
                        tempColorLeftUp.Z = array[x - 1 + ((y - 1) * width)].B;

                        tempColorLeftDown.X = array[x - 1 + ((y + 1) * width)].R;
                        tempColorLeftDown.Y = array[x - 1 + ((y + 1) * width)].G;
                        tempColorLeftDown.Z = array[x - 1 + ((y + 1) * width)].B;

                        tempColorRightUp.X = array[x + 1 + ((y - 1) * width)].R;
                        tempColorRightUp.Y = array[x + 1 + ((y - 1) * width)].G;
                        tempColorRightUp.Z = array[x + 1 + ((y - 1) * width)].B;

                        tempColorRightDown.X = array[x + 1 + ((y + 1) * width)].R;
                        tempColorRightDown.Y = array[x + 1 + ((y + 1) * width)].G;
                        tempColorRightDown.Z = array[x + 1 + ((y + 1) * width)].B;

                        Vector3 newVectorColor = (originalColor * (1 - weight))
                                               + (((tempColorLeft + tempColorRight + tempColorUp + tempColorDown + tempColorLeftUp + tempColorLeftDown + tempColorRightUp + tempColorRightDown) / 8) * weight);

                        Color returnColor = new Color(newVectorColor.X / 255, newVectorColor.Y / 255, newVectorColor.Z / 255);

                        array[x + (y * width)] = returnColor;
                    }
                }
            }

            return array;
        }

        public static Color[] ContrastBlurArray(Color[] array, int width, int height, float weight, float contrast)
        {
            for (int y = 0; y < height; y++)
            {
                Color[] originalArray = array;
                for (int x = 0; x < width; x++)
                {
                    if (y > 0 && y < height - 1 && x > 0 && x < width - 1)
                    {
                        List<Vector3> colorsToBlur = new List<Vector3>();
                        List<StoreColorBufferVector> identities = new List<StoreColorBufferVector>();
                        
                        Vector3 originalColor;
                        Vector3 grayscaleOriginal;
                        Vector3 tempColorLeft;
                        Vector3 grayscaleLeft;
                        Vector3 tempColorRight;
                        Vector3 grayscaleRight;
                        Vector3 tempColorUp;
                        Vector3 grayscaleUp;
                        Vector3 tempColorDown;
                        Vector3 grayscaleDown;
                        Vector3 tempColorLeftUp;
                        Vector3 grayscaleLeftUp;
                        Vector3 tempColorRightUp;
                        Vector3 grayscaleRightUp;
                        Vector3 tempColorLeftDown;
                        Vector3 grayscaleLeftDown;
                        Vector3 tempColorRightDown;
                        Vector3 grayscaleRightDown;

                        float mixdown;

                        weight = Math.Clamp(weight, 0, 1);

                        originalColor.X = originalArray[x + (y * width)].R;
                        originalColor.Y = originalArray[x + (y * width)].G;
                        originalColor.Z = originalArray[x + (y * width)].B;
                        mixdown = originalColor.X * 0.3f + originalColor.Y * 0.6f + originalColor.Z * 0.11f;
                        grayscaleOriginal = new Vector3(mixdown, mixdown, mixdown);

                        tempColorLeft.X = originalArray[x - 1 + (y * width)].R;
                        tempColorLeft.Y = originalArray[x - 1 + (y * width)].G;
                        tempColorLeft.Z = originalArray[x - 1 + (y * width)].B;
                        mixdown = tempColorLeft.X * 0.3f + tempColorLeft.Y * 0.6f + tempColorLeft.Z * 0.11f;
                        grayscaleLeft = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleLeft, tempColorLeft));

                        tempColorRight.X = originalArray[x + 1 + (y * width)].R;
                        tempColorRight.Y = originalArray[x + 1 + (y * width)].G;
                        tempColorRight.Z = originalArray[x + 1 + (y * width)].B;
                        mixdown = tempColorRight.X * 0.3f + tempColorRight.Y * 0.6f + tempColorRight.Z * 0.11f;
                        grayscaleRight = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleRight, tempColorRight));

                        tempColorUp.X = originalArray[x + ((y - 1) * width)].R;
                        tempColorUp.Y = originalArray[x + ((y - 1) * width)].G;
                        tempColorUp.Z = originalArray[x + ((y - 1) * width)].B;
                        mixdown = tempColorUp.X * 0.3f + tempColorUp.Y * 0.6f + tempColorUp.Z * 0.11f;
                        grayscaleUp = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleUp, tempColorUp));

                        tempColorDown.X = originalArray[x + ((y + 1) * width)].R;
                        tempColorDown.Y = originalArray[x + ((y + 1) * width)].G;
                        tempColorDown.Z = originalArray[x + ((y + 1) * width)].B;
                        mixdown = tempColorDown.X * 0.3f + tempColorDown.Y * 0.6f + tempColorDown.Z * 0.11f;
                        grayscaleDown = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleDown, tempColorDown));

                        tempColorLeftUp.X = originalArray[x - 1 + ((y - 1) * width)].R;
                        tempColorLeftUp.Y = originalArray[x - 1 + ((y - 1) * width)].G;
                        tempColorLeftUp.Z = originalArray[x - 1 + ((y - 1) * width)].B;
                        mixdown = tempColorLeftUp.X * 0.3f + tempColorLeftUp.Y * 0.6f + tempColorLeftUp.Z * 0.11f;
                        grayscaleLeftUp = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleLeftUp, tempColorLeftUp));

                        tempColorLeftDown.X = originalArray[x - 1 + ((y + 1) * width)].R;
                        tempColorLeftDown.Y = originalArray[x - 1 + ((y + 1) * width)].G;
                        tempColorLeftDown.Z = originalArray[x - 1 + ((y + 1) * width)].B;
                        mixdown = tempColorLeftDown.X * 0.3f + tempColorLeftDown.Y * 0.6f + tempColorLeftDown.Z * 0.11f;
                        grayscaleLeftDown = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleLeftDown, tempColorLeftDown));

                        tempColorRightUp.X = originalArray[x + 1 + ((y - 1) * width)].R;
                        tempColorRightUp.Y = originalArray[x + 1 + ((y - 1) * width)].G;
                        tempColorRightUp.Z = originalArray[x + 1 + ((y - 1) * width)].B;
                        mixdown = tempColorRightUp.X * 0.3f + tempColorRightUp.Y * 0.6f + tempColorRightUp.Z * 0.11f;
                        grayscaleRightUp = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleRightUp, tempColorRightUp));

                        tempColorRightDown.X = originalArray[x + 1 + ((y + 1) * width)].R;
                        tempColorRightDown.Y = originalArray[x + 1 + ((y + 1) * width)].G;
                        tempColorRightDown.Z = originalArray[x + 1 + ((y + 1) * width)].B;
                        mixdown = tempColorRightDown.X * 0.3f + tempColorRightDown.Y * 0.6f + tempColorRightDown.Z * 0.11f;
                        grayscaleRightDown = new Vector3(mixdown, mixdown, mixdown);
                        identities.Add(new StoreColorBufferVector(grayscaleRightDown, tempColorRightDown));

                        foreach (StoreColorBufferVector identity in identities)
                        {
                            if (grayscaleOriginal.X >= identity.grayscale.X)
                            {
                                if (Math.Abs(grayscaleOriginal.X - identity.grayscale.X) > contrast)
                                {
                                    colorsToBlur.Add(identity.original);
                                }
                            }
                            else
                            {
                                if (Math.Abs(identity.grayscale.X - grayscaleOriginal.X) > contrast)
                                {
                                    colorsToBlur.Add(identity.original);
                                }
                            }
                        }

                        Vector3 blurColor;
                        if (colorsToBlur.Count != 0)
                        {
                            blurColor = colorsToBlur[0];
                            foreach (Vector3 color in colorsToBlur)
                            {
                                if (color != colorsToBlur[0])
                                {
                                    blurColor /= 2;
                                    blurColor += color / 2;
                                }
                            }
                        }
                        else
                            blurColor = originalColor;

                        Vector3 newVectorColor = (originalColor * (1 - weight))
                                               + (blurColor * weight);

                        Color returnColor = new Color(newVectorColor.X / 255, newVectorColor.Y / 255, newVectorColor.Z / 255);

                        array[x + (y * width)] = returnColor;
                    }
                }
            }

            return array;
        }

        public static Color[] Despeckle(Color[] array, int width, int height, float weight, float threshold)
        {
            for (int y = 0; y < height; y++)
            {
                Color[] originalArray = array;
                for (int x = 0; x < width; x++)
                {
                    if (y > 0 && y < height - 1 && x > 0 && x < width - 1)
                    {
                        Vector3 originalColor;
                        Vector3 grayscaleOriginal;
                        Vector3 tempColorLeft;
                        Vector3 tempColorRight;
                        Vector3 tempColorUp;
                        Vector3 tempColorDown;
                        Vector3 tempColorLeftUp;
                        Vector3 tempColorRightUp;
                        Vector3 tempColorLeftDown;
                        Vector3 tempColorRightDown;

                        float mixdown;

                        weight = Math.Clamp(weight, 0, 1);

                        originalColor.X = (float)originalArray[x + (y * width)].R;
                        originalColor.Y = (float)originalArray[x + (y * width)].G;
                        originalColor.Z = (float)originalArray[x + (y * width)].B;
                        mixdown = originalColor.X * 0.3f + originalColor.Y * 0.6f + originalColor.Z * 0.11f;
                        grayscaleOriginal = new Vector3(mixdown, mixdown, mixdown);

                        tempColorLeft.X = originalArray[x - 1 + (y * width)].R;
                        tempColorLeft.Y = originalArray[x - 1 + (y * width)].G;
                        tempColorLeft.Z = originalArray[x - 1 + (y * width)].B;

                        tempColorRight.X = originalArray[x + 1 + (y * width)].R;
                        tempColorRight.Y = originalArray[x + 1 + (y * width)].G;
                        tempColorRight.Z = originalArray[x + 1 + (y * width)].B;

                        tempColorUp.X = originalArray[x + ((y - 1) * width)].R;
                        tempColorUp.Y = originalArray[x + ((y - 1) * width)].G;
                        tempColorUp.Z = originalArray[x + ((y - 1) * width)].B;

                        tempColorDown.X = originalArray[x + ((y + 1) * width)].R;
                        tempColorDown.Y = originalArray[x + ((y + 1) * width)].G;
                        tempColorDown.Z = originalArray[x + ((y + 1) * width)].B;

                        tempColorLeftUp.X = originalArray[x - 1 + ((y - 1) * width)].R;
                        tempColorLeftUp.Y = originalArray[x - 1 + ((y - 1) * width)].G;
                        tempColorLeftUp.Z = originalArray[x - 1 + ((y - 1) * width)].B;

                        tempColorLeftDown.X = originalArray[x - 1 + ((y + 1) * width)].R;
                        tempColorLeftDown.Y = originalArray[x - 1 + ((y + 1) * width)].G;
                        tempColorLeftDown.Z = originalArray[x - 1 + ((y + 1) * width)].B;

                        tempColorRightUp.X = originalArray[x + 1 + ((y - 1) * width)].R;
                        tempColorRightUp.Y = originalArray[x + 1 + ((y - 1) * width)].G;
                        tempColorRightUp.Z = originalArray[x + 1 + ((y - 1) * width)].B;

                        tempColorRightDown.X = originalArray[x + 1 + ((y + 1) * width)].R;
                        tempColorRightDown.Y = originalArray[x + 1 + ((y + 1) * width)].G;
                        tempColorRightDown.Z = originalArray[x + 1 + ((y + 1) * width)].B;

                        if (grayscaleOriginal.X <= 0 || grayscaleOriginal.X > threshold)
                        {
                            Vector3 blurColor = (originalColor * (1 - weight))
                                              + (((tempColorLeft + tempColorRight + tempColorUp + tempColorDown + tempColorLeftUp + tempColorLeftDown + tempColorRightUp + tempColorRightDown) / 8) * weight);

                            Vector3 newVectorColor = (originalColor * (1 - weight))
                                                   + (blurColor * weight);

                            Color returnColor = new Color(newVectorColor.X / 255, newVectorColor.Y / 255, newVectorColor.Z / 255);

                            array[x + (y * width)] = returnColor;
                        }
                    }
                }
            }

            return array;
        }
    }

    public class StoreColorBufferVector
    {
        public StoreColorBufferVector(Vector3 inputGrayscale, Vector3 inputOriginal)
        {
            grayscale = inputGrayscale;
            original = inputOriginal;
        }
        public Vector3 grayscale;
        public Vector3 original;
    }
}