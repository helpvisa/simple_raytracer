﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raytracing;
using PixelManagement;
using MiscFunctions;

namespace simpleRaytracer
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch spriteBatch;

        // the screen buffers
        Texture2D buffer1;

        // screen width and height + screen buffer array, global tick
        int width;
        int height;
        int globalWidth;
        int globalHeight;
        Color[] buffer1Data;

        // aspect ratio
        float aspectRatio;

        // create global camera and world, and lights
        Camera camera;
        SurfaceList world = new SurfaceList();
        LightList lights = new LightList();
        //Light mainLight = new PointLight(new Vector3(6f, -6f, 5f), 1f);
        //Light mainLight = new Light(new Vector3(0.95f, -8f, 0f), 1);


        // define how many samples to cast per pixel, and how deep each recursive child ray can go
        int samples = 50;
        int maxDepth = 4;
        Random random = new Random();

        // variables for mixing buffer at the end of render
        bool justSorted = false;
        bool blurOutput = true;
        int blurPasses = 0;
        float blurWeight = 1f; //0.15f;
        //float contrastWeight = 0.125f;
        float speckleThreshold = 200;
        bool saveImage = true;

        // threads
        Thread[] threadArray;
        List<Color[]> bufferList = new List<Color[]>();
        int numberOfAvailableThreads;
        bool isDone = false;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // set timestep and stuff, for unlocked update
            _graphics.SynchronizeWithVerticalRetrace = true;
            IsFixedTimeStep = false;

            

            
            // set width and height based on window size
            //_graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            //_graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            //_graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = 480;
            _graphics.PreferredBackBufferHeight = 270;
            _graphics.ApplyChanges();

            width = _graphics.PreferredBackBufferWidth;
            height = _graphics.PreferredBackBufferHeight;
            globalWidth = width;
            globalHeight = height;
            aspectRatio = (float)width / (float)height;

            // prepare for threading
            numberOfAvailableThreads = Environment.ProcessorCount - 1;
            if (numberOfAvailableThreads < 1)
                numberOfAvailableThreads = 1;
            threadArray = new Thread[numberOfAvailableThreads];
            
            // initialize the camera
            //camera = new Camera(Vector3.Zero, new Vector3(0,0,-1), 65, aspectRatio);
            //camera = new Camera(new Vector3(0,0,0), new Vector3(-0.03f, 0.875f, -7.6f), 63, aspectRatio);
            //camera = new Camera(new Vector3(-16.5f,-9.55f,10), new Vector3(-0.8f, 1.25f, -7.6f), 15, aspectRatio);
            camera = new Camera(new Vector3(-2,-1.2f,2), new Vector3(-1,0,-3), 52, aspectRatio);

            // define materials //
            // spheres
            Material mat1 = new Material(new Vector3(0.2f, 0.2f, 1f), 0f, 0.965f, Vector3.Zero);
            Material mat2 = new Material(new Vector3(1f, 0.65f, 0f), 1f, 0.65f, Vector3.Zero);
            Material mat3 = new Material(new Vector3(1f, 1f, 1f), 0f, 0.15f, Vector3.Zero);
            // emissives
            Material mat4 = new Material(new Vector3(0,0,0), 0, 0f, new Vector3(25f,0.5f,0.5f));
            Material mat5 = new Material(new Vector3(0,0,0), 0, 0f, new Vector3(0.5f,0.5f,25f));
            // tris
            Material mat6 = new Material(new Vector3(1f,0f,0f), 1f, 0.35f, Vector3.Zero);

            // initialize surfaces
            world.surfaces.Add(new Sphere(new Vector3(2.05f, 0.9f, -7.25f), 3.5f, mat1));
            world.surfaces.Add(new Sphere(new Vector3(-4.5f, 2f, -8f), 2.3f, mat2));
            world.surfaces.Add(new Sphere(new Vector3(0,45,-20f), 42.5f, mat3));
            //world.surfaces.Add(new Sphere(new Vector3(-1.5f,3.75f,-7.5f), 0.5f, mat5));

            // rect testing //
            //world.surfaces.Add(new RectAxis(-6, -3, -4, 4, -20, mat4));

            // tri testing //
            // create verts
            Vert v0 = new Vert(new Vector3(0, 0,-3), Vector3.Normalize(new Vector3(0,0,1)));
            Vert v1 = new Vert(new Vector3(-3,0,-6), Vector3.Normalize(new Vector3(-1,0,0)));
            Vert v2 = new Vert(new Vector3(0,0,-9), Vector3.Normalize(new Vector3(0,0,-1)));
            Vert v3 = new Vert(new Vector3(3,0,-6), Vector3.Normalize(new Vector3(1,0,0)));
            Vert v4 = new Vert(new Vector3(0,-3,-6), Vector3.Normalize(new Vector3(0,-1,0)));
            Vert v5 = new Vert(new Vector3(0,3,-6), Vector3.Normalize(new Vector3(0,1,0)));
            // create tris
            world.surfaces.Add(new Tri(v0, v1, v5, mat6, true, false));
            world.surfaces.Add(new Tri(v0, v1, v4, mat6, false));
            world.surfaces.Add(new Tri(v0, v3, v5, mat6, false));
            world.surfaces.Add(new Tri(v0, v3, v4, mat6, true, false));
            world.surfaces.Add(new Tri(v2, v1, v5, mat6, false));
            world.surfaces.Add(new Tri(v2, v1, v4, mat6, true, false));
            world.surfaces.Add(new Tri(v2, v3, v5, mat6, true, false));
            world.surfaces.Add(new Tri(v2, v3, v4, mat6, false));
            

            lights.lights.Add(new PointLight(new Vector3(6f, 2f, 3f), 2f));//, new Vector3(1.25f,0,0)));
            lights.lights.Add(new PointLight(new Vector3(0f, 0f, -12f), 0.5f));
            lights.lights.Add(new PointLight(new Vector3(-3f, 5f, -0.5f), 0.21f));//, new Vector3(0,1.25f,0)));
            lights.lights.Add(new PointLight(new Vector3(0f,-8f,0f), 0.8f));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // create array and texture to blit colours into
            buffer1 = new Texture2D(GraphicsDevice, width, height);
            buffer1Data = new Color[width * height];
            
            // thread testing
            /* technique 1 (split screen into sections)
                if (numberOfAvailableThreads > 1)
                {
                    Parallel.For(0, numberOfAvailableThreads, startThread);
                    //{
                        //threadArray[i] = new Thread(() => renderBlock(width, (height / (numberOfAvailableThreads-1)) * (i-1), (height / (numberOfAvailableThreads-1)) * (i-2), i - 1, new Random()));
                        //threadArray[i].Start();
                    //}
                }
                else
                {
                    threadArray[0] = new Thread(() => renderBlock(width, height, 0, 1, random));
                    threadArray[0].Start();
                }
            */

            // technique 2 (render whole screen numerous times into separate buffers w diff random seed, combine at end)
            Parallel.For(0, numberOfAvailableThreads, startThreadFull);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                for (int i = 0; i < threadArray.Length; i++)
                    threadArray[i].Abort();
            }

            bool stillRunning = false;
            for (int i = 0; i < threadArray.Length; i++)
            {
                stillRunning = threadArray[i].IsAlive;
                if (stillRunning)
                {
                    // update the texture with the buffer array (old)
                    buffer1.SetData<Color>(bufferList[0]);
                    return;
                }
            }
            
            if (!stillRunning && !isDone)
            {
                Console.WriteLine("Done!");
                Console.WriteLine("Render took " + gameTime.TotalGameTime + ".");
                isDone = true;

                // update texture w all buffers (new!)
                foreach (Color[] buffer in bufferList)
                {
                    for (int y = 0; y < globalHeight; y++)
                        for (int x = 0; x < globalWidth; x++)
                        {
                            Vector3 tempBuffCol = buffer1Data[x + (y * width)].ToVector3();
                            Vector3 currBuffCol = buffer[x + (y * width)].ToVector3();
                            Vector3 newBuffCol = (tempBuffCol / 2) + (currBuffCol / 2);
                            buffer1Data[x + (y * width)] = new Color(newBuffCol.X, newBuffCol.Y, newBuffCol.Z);
                        }
                }
                buffer1.SetData<Color>(buffer1Data);
                Console.Write("Mixed threaded buffers. ");
            }

            if (!justSorted && blurOutput && isDone)
            {
                for (int i = 0; i < blurPasses; i++)
                {
                    buffer1Data = Sorting.Despeckle(buffer1Data, width, height, blurWeight, speckleThreshold);
                    buffer1.SetData<Color>(buffer1Data);
                    Console.WriteLine("Completed despeckle pass #" + (i + 1) + ".");
                }

                justSorted = true;
                if (blurPasses > 0)
                    Console.WriteLine("Despeckled output image. ");
                
                if (saveImage)
                {
                    var stream = new FileStream("renders/" + DateTime.Now.ToString("hh/mm/ss/dd/MM/yyyy") + ".png", FileMode.Create);
                    buffer1.SaveAsPng(stream, globalWidth, globalHeight);
                    Console.WriteLine("Saved rendered image to " + stream.Name + ".");
                }
            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {   
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            // draw the buffer to the screen
            spriteBatch.Begin();
            spriteBatch.Draw(buffer1, Vector2.Zero, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
        
        // render block function for threading; probably split this out later
        void renderBlock(int width, int height, int y, int threadNumber, Random random)
        {
            // iterate through scanlines
            while (y < height)
            {
                // print which scanline is currently being evaluated
                Console.WriteLine("Scanlines remaining on Thread " + threadNumber + ": " + (height - y));

                // iterate through x on scanline
                for(int x = 0; x < width; x++)
                {
                    // create the color variable for the ray
                    Color rayColor;
                    Vector3 vectorRayColor = Vector3.Zero;

                    // get the coordinates on the camera viewport, and cast a ray through it, jittering by a random number
                    for (int i = 0; i < samples; i++)
                    {
                        float u = (float)(x + (float)random.Next(0, 100) / 100) / globalWidth;
                        float v = (float)(y + (float)random.Next(0, 100) / 100) / globalHeight;

                        // create the ray
                        CustomRay ray = new CustomRay(camera.position, camera.lowerLeftCorner + u * camera.horizontal + v * camera.vertical - camera.position);

                        // intersect ray against bg and objects
                        //Vector3 vectorColor1 = RayOperations.GetRayNormalColor(ray, world);
                        //Vector3 vectorColor1 = RayOperations.GetRayDepthColor(ray, world);
                        //Vector3 vectorColor1 = RayOperations.GetLights(ray, world, random, lights);
                        Vector3 vectorColor1 = RayOperations.GetRayColor(ray, world, random, maxDepth, lights);

                        vectorRayColor += vectorColor1 / samples;
                    }

                    // write the ray color to the pixel
                    //vectorRayColor /= samples;
                    // gamma correction
                    vectorRayColor.X = (float)Math.Sqrt(vectorRayColor.X);
                    vectorRayColor.Y = (float)Math.Sqrt(vectorRayColor.Y);
                    vectorRayColor.Z = (float)Math.Sqrt(vectorRayColor.Z);

                    rayColor = new Color(vectorRayColor.X, vectorRayColor.Y, vectorRayColor.Z);
                    PixelOperations.WritePixel(buffer1Data, x, y, width, rayColor);
                }
                // increment y
                y++;
            }
            
            if (y >= height)
            {
                Console.WriteLine("Thread " + threadNumber + " is done rendering.");
                Thread.Sleep(0);
            }
        }

        // render block function for threading; probably split this out later
        void renderBlockFull(int width, int height, int threadNumber, Random random, Color[] buffer)
        {
            int y =0;

            // iterate through scanlines
            while (y < height)
            {
                // print which scanline is currently being evaluated
                Console.WriteLine("Scanlines remaining on Thread " + threadNumber + ": " + (height - y));

                // iterate through x on scanline
                for(int x = 0; x < width; x++)
                {
                    // create the color variable for the ray
                    Color rayColor;
                    Vector3 vectorRayColor = Vector3.Zero;

                    // get the coordinates on the camera viewport, and cast a ray through it, jittering by a random number
                    for (int i = 0; i < samples; i++)
                    {
                        float u = (float)(x + (float)random.Next(0, 100) / 100) / globalWidth;
                        float v = (float)(y + (float)random.Next(0, 100) / 100) / globalHeight;

                        // create the ray (flat projection)
                        CustomRay ray = camera.returnRay(u, v);

                        // intersect ray against bg and objects
                        //Vector3 vectorColor1 = RayOperations.GetRayNormalColor(ray, world);
                        //Vector3 vectorColor1 = RayOperations.GetRayDepthColor(ray, world);
                        //Vector3 vectorColor1 = RayOperations.GetLights(ray, world, random, lights);
                        Vector3 vectorColor1 = RayOperations.GetRayColor(ray, world, random, maxDepth, lights);

                        vectorRayColor += vectorColor1 / samples;
                    }

                    // write the ray color to the pixel
                    //vectorRayColor /= samples;
                    // gamma correction
                    vectorRayColor.X = (float)Math.Sqrt(vectorRayColor.X);
                    vectorRayColor.Y = (float)Math.Sqrt(vectorRayColor.Y);
                    vectorRayColor.Z = (float)Math.Sqrt(vectorRayColor.Z);

                    rayColor = new Color(vectorRayColor.X, vectorRayColor.Y, vectorRayColor.Z);
                    PixelOperations.WritePixel(buffer, x, y, width, rayColor);
                }
                // increment y
                y++;
            }
            
            if (y >= height)
            {
                Console.WriteLine("Thread " + threadNumber + " is done rendering.");
                Thread.Sleep(0);
            }
        }

        void startThread(int i)
        {
            threadArray[i] = new Thread(() => renderBlock(width, (height / (numberOfAvailableThreads)) * (i+1), (height / (numberOfAvailableThreads)) * (i), i + 1, new Random()));
            threadArray[i].Start();
        }

        void startThreadFull(int i)
        {
            Color[] newBuffer = new Color[globalWidth * globalHeight];
            bufferList.Add(newBuffer);
            threadArray[i] = new Thread(() => renderBlockFull(globalWidth, globalHeight, i + 1, new Random(), newBuffer));
            threadArray[i].Start();
        }
    }
}
