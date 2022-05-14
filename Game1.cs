using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Raytracing;

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

        // create global camera
        Camera camera;
        Sphere testSphere;


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
            camera = new Camera(new Vector3(0,0,0), new Vector3(0,0,1), 1f, 2, aspectRatio);

            // initialize spheres
            testSphere = new Sphere(new Vector3(0f, 0f, -2f), 1f);

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
                // print which scaline is currently being evaluated
                Console.WriteLine("Scanlines remaining: " + (height - y));

                // iterate through x on scanline
                for (int x = 0; x < width; x++)
                {
                    // and draw pixels based on xy position
                    //buffer1Data[x + (y * width)] = new Color(((float)x / width), ((float)y / height), 0);

                    // get the coordinates on the camera viewport, and cast a ray through it
                    float u = (float)x / width;
                    float v = (float)y / height;

                    // create the ray
                    CustomRay ray = new CustomRay(camera.position, camera.lowerLeftCorner + u*camera.horizontal + v*camera.vertical - camera.position);

                    // create the color variable for the ray
                    Color rayColor;

                    // intersect ray against bg and objects
                    Vector3 vectorColor = RayFunctions.GetRayColor(ray, testSphere);

                    rayColor = new Color(vectorColor.X, vectorColor.Y, vectorColor.Z);

                    // write the ray color to the pixel
                    buffer1Data[x + (y * width)] = rayColor;
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
