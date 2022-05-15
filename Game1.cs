using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Raytracing;
using PixelManagement;

namespace simpleRaytracer
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch spriteBatch;

        Texture2D buffer1;

        // screen width and height + screen buffer array
        int width;
        int height;
        Color[] buffer1Data;

        // aspect ratio
        float aspectRatio;

        // create global camera and world
        Camera camera;
        SurfaceList world = new SurfaceList();

        // define how many samples to cast per pixel, and how deep each recursive child ray can go
        int samples = 150;
        int maxDepth = 8;
        Random random = new Random();


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // set width and height based on window size
            width = Window.ClientBounds.Width;
            height = Window.ClientBounds.Height;
            aspectRatio = (float)width / (float)height;
            
            // initialize the camera
            camera = new Camera(new Vector3(0,0,0), new Vector3(0,0,1f), 1.5f, 2, aspectRatio);

            // initialize surfaces
            world.surfaces.Add(new Sphere(new Vector3(1f, -0.45f, -3f), 1.2f));
            world.surfaces.Add(new Sphere(new Vector3(-4.5f, 2f, -8f), 2.3f));
            world.surfaces.Add(new Sphere(new Vector3(0,100,-5.5f), 95.5f));

            // initialize lights
            Light mainLight = new Light(new Vector3(0, 4, -1.7f), 1f);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // create array and texture to blit colours into
            buffer1 = new Texture2D(GraphicsDevice, width, height);
            buffer1Data = new Color[width * height];
            
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
                        Vector3 vectorColor = RayOperations.GetRayColor(ray, world, random, maxDepth);
                        
                        vectorRayColor += vectorColor;
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
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

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
