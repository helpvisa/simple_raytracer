using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using System.Collections.Generic;
using Raytracing;
using Assimp;
using Assimp.Configs;

namespace ModelLoader
{
    public static class ModelOperations
    {
        public static Scene LoadModel(string loadPath)
        {
            // create the importer
            AssimpContext importer = new AssimpContext();

            // create smoothing config
            NormalSmoothingAngleConfig smoothing = new NormalSmoothingAngleConfig(66.6f);
            importer.SetConfig(smoothing);

            // logging callback
            LogStream logstream = new LogStream(delegate(String msg, String userData) {
                Console.WriteLine(msg);
            });
            logstream.Attach();

            // import desired model // gotta fix node transformations
            Scene model = importer.ImportFile(loadPath, PostProcessSteps.PreTransformVertices);

            importer.Dispose();

            return model;
        }
        
        public static void CreateModel(Scene model, Raytracing.Material inMat, SurfaceList world, Vector3 offset)
        {
            List<Mesh> meshes = model.Meshes;
            foreach (Mesh mesh in meshes)
            {
                List<int> indexBuffer = new List<int>();
                List<Vert> vertexBuffer = new List<Vert>();
                List<Face> faces = mesh.Faces;
                List<Vector3D> vertexList = mesh.Vertices;
                List<Vector3D> normalList = mesh.Normals;

                for(int i = 0; i < vertexList.Count; i++)
                {
                    Vector3 pos = new Vector3(vertexList[i].X + offset.X, vertexList[i].Y + offset.Y, vertexList[i].Z + offset.Z);
                    Vector3 nor = Vector3.Normalize(new Vector3(normalList[i].X, normalList[i].Y, normalList[i].Z));
                    vertexBuffer.Add(new Vert(pos, nor));
                }

                foreach(Face face in faces)
                {
                    List<int> indices = face.Indices;
                    world.surfaces.Add(new Tri(vertexBuffer[indices[0]], vertexBuffer[indices[1]], vertexBuffer[indices[2]], inMat, false, true));
                }
                //Console.Write(vertexBuffer.Count);
            }
        }

        public static Raytracing.Camera CreateScene(Scene model, Raytracing.Material inMat, SurfaceList world, LightList lights, float ratio)
        {
            // load models from scene
            Node rootNode = model.RootNode;
            List<Mesh> meshes = model.Meshes;

            // handle meshes
            foreach (Mesh mesh in meshes)
            {
                List<int> indexBuffer = new List<int>();
                List<Vert> vertexBuffer = new List<Vert>();
                List<Face> faces = mesh.Faces;
                List<Vector3D> vertexList = mesh.Vertices;
                List<Vector3D> normalList = mesh.Normals;

                for(int i = 0; i < vertexList.Count; i++)
                {
                    Vector3 pos = new Vector3(vertexList[i].X, vertexList[i].Y, vertexList[i].Z);
                    Vector3 nor = Vector3.Normalize(new Vector3(normalList[i].X, normalList[i].Y, normalList[i].Z));
                    vertexBuffer.Add(new Vert(pos, nor));
                }

                foreach(Face face in faces)
                {
                    List<int> indices = face.Indices;
                    world.surfaces.Add(new Tri(vertexBuffer[indices[0]], vertexBuffer[indices[1]], vertexBuffer[indices[2]], inMat, false, true));
                }
            }

            // load lights
            foreach (Assimp.Light light in model.Lights)
            {
                Vector3 position = new Vector3(light.Position.X, light.Position.Y, light.Position.Z);
                lights.lights.Add(new PointLight(position, light.ColorDiffuse.R / 125));
            }

            // load camera
            Raytracing.Camera cam = new Raytracing.Camera(Vector3.Zero, new Vector3(0,0,-1), 72, ratio);
            cam = new Raytracing.Camera(new Vector3(0,0,6), Vector3.Zero, 29, ratio);
            foreach (Assimp.Camera camera in model.Cameras)
            {

                Vector3 pos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);
                Vector3 direction = new Vector3(camera.Direction.X, camera.Direction.Y, camera.Direction.Z);
                float fov = MathHelper.ToDegrees(camera.FieldOfview) / 2.25f;

                Vector3 lookAt = new Vector3(pos.X + direction.X, pos.Y + direction.Y, pos.Z + direction.Z);
                cam = new Raytracing.Camera(pos, -lookAt, fov, ratio);
            }

            // finish and return scene camera for render
            return cam;
        }
    }
}