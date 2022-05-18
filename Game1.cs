using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
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

        // screen width and height + screen buffer array
        int width;
        int height;
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
        int samples = 200;
        int maxDepth = 6;
        Random random = new Random();

        // global y for incremental update version of program
        int y = 0;

        // variables for mixing buffer at the end of render
        bool justSorted = false;
        bool blurOutput = true;
        int blurPasses = 4;
        float blurWeight = 1f; //0.15f;
        //float contrastWeight = 0.125f;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // set timestep and stuff, for unlocked update
            _graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            
            // set width and height based on window size
            width = Window.ClientBounds.Width;
            height = Window.ClientBounds.Height;
            aspectRatio = (float)width / (float)height;
            
            // initialize the camera
            camera = new Camera(new Vector3(0,0,0), new Vector3(0,0,1f), 1.5f, 2, aspectRatio);

            // define materials
            Material mat1 = new Material(new Vector3(0.2f, 0.8f, 0.6f), 0f, 0.95f, Vector3.Zero);
            Material mat2 = new Material(new Vector3(0.5f, 0.15f, 0.5f), 1f, 0.55f, Vector3.Zero);
            Material mat3 = new Material(new Vector3(0.28f, 0.15f, 0.05f), 0f, 0f, Vector3.Zero);

            // initialize surfaces
            world.surfaces.Add(new Sphere(new Vector3(1f, -0.45f, -3f), 1.2f, mat1));
            world.surfaces.Add(new Sphere(new Vector3(-4.5f, 2f, -8f), 2.3f, mat2));
            world.surfaces.Add(new Sphere(new Vector3(0,45,-20f), 42.5f, mat3));

            lights.lights.Add(new PointLight(new Vector3(6f, -6f, 5f), 0.35f));
            lights.lights.Add(new PointLight(new Vector3(0.95f, -8f, -10f), 0.075f));
            lights.lights.Add(new PointLight(new Vector3(-3f, 5f, -0.5f), 0.15f));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // create array and texture to blit colours into
            buffer1 = new Texture2D(GraphicsDevice, width, height);
            buffer1Data = new Color[width * height];

            // disabled to move this into update loop for "dynamic" visual refresh
            /*
            // iterate through scanlines
            for (int y = 0; y < height; y++)
            {
                // print which scanline is currently being evaluated
                Console.WriteLine("Scanlines remaining: " + (height - y));

                // iterate through x on scanline
                for (int x = 0; x < width; x++)
                {
                    // create the color variable for the ray
                    Color rayColor;
                    Vector3 vectorRayColor = Vector3.Zero;
                    
                    // get the coordinates on the camera viewport, and cast a ray through it, jittering by a random number
                    for (int i = 0; i < samples; i++)
                    {
                        float u = (float)(x + (float)random.Next(0,100) / 100) / width;
                        float v = (float)(y + (float)random.Next(0,100) / 100) / height;

                        // create the ray
                        CustomRay ray = new CustomRay(camera.position, camera.lowerLeftCorner + u * camera.horizontal + v * camera.vertical - camera.position);

                        // intersect ray against bg and objects
                        //Vector3 vectorColor = RayOperations.GetRayNormalColor(ray, world);
                        //Vector3 vectorColor = RayOperations.GetRayDepthColor(ray, world);
                        Vector3 vectorColor1 = RayOperations.GetRayColor(ray, world, random, maxDepth);
                        
                        vectorRayColor += vectorColor1;
                    }

                    // write the ray color to the pixel
                    vectorRayColor /= samples;
                    // gamma correction
                    vectorRayColor.X = (float)Math.Sqrt(vectorRayColor.X);
                    vectorRayColor.Y = (float)Math.Sqrt(vectorRayColor.Y);
                    vectorRayColor.Z = (float)Math.Sqrt(vectorRayColor.Z);

                    rayColor = new Color(vectorRayColor.X, vectorRayColor.Y, vectorRayColor.Z);
                    PixelOperations.WritePixel(buffer1Data, x, y, width, rayColor);
                }
            }
            
            // print if successful
            Console.WriteLine("Done!");
            // update the texture with the buffer array
            buffer1.SetData<Color>(buffer1Data);

            // print aspect ratio; for testing and debug
            //Console.WriteLine(aspectRatio);
            */

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // iterate through scanlines
            if (y < height)
            {
                // print which scanline is currently being evaluated
                 Console.WriteLine("Scanlines remaining: " + (height - y));

                // iterate through x on scanline
                for (int x = 0; x < width; x++)
                {
                    // create the color variable for the ray
                    Color rayColor;
                    Vector3 vectorRayColor = Vector3.Zero;
                    
                    // get the coordinates on the camera viewport, and cast a ray through it, jittering by a random number
                    for (int i = 0; i < samples; i++)
                    {
                        float u = (float)(x + (float)random.Next(0,100) / 100) / width;
                        float v = (float)(y + (float)random.Next(0,100) / 100) / height;

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
                // increment global y
                y += 1;

                // update the texture with the buffer array
                buffer1.SetData<Color>(buffer1Data);
                
                // say if done
                if (y >= height)
                {
                    Console.WriteLine("Done!");
                    Console.WriteLine("Render took " + gameTime.TotalGameTime + ".");

                    if (!justSorted && blurOutput)
                    {
                        for (int i = 0; i < blurPasses; i++)
                        {
                            buffer1Data = Sorting.Despeckle(buffer1Data, width, height, blurWeight);
                            buffer1.SetData<Color>(buffer1Data);
                            Console.WriteLine("Completed despeckle pass #" + (i + 1) + ".");
                        }

                        justSorted = true;
                        if (blurPasses > 0)
                            Console.WriteLine("Despeckled output image.");
                    }
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
    }
}
