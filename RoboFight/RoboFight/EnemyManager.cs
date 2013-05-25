using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Spine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TiledLib;

namespace RoboFight
{
    class EnemyManager
    {
        public List<Robot> Enemies = new List<Robot>();

        Texture2D blankTex;

        SkeletonRenderer skeletonRenderer;

        Dictionary<string, Atlas> AtlasDict = new Dictionary<string, Atlas>();
        Dictionary<string, string> JsonDict = new Dictionary<string, string>();

        static Random rand = new Random();

        public EnemyManager()
        { }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            blankTex = content.Load<Texture2D>("blank");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);

            AtlasDict.Add("robo", new Atlas(graphicsDevice, Path.Combine(content.RootDirectory, "robo/robo.atlas")));
            JsonDict.Add("robo", File.ReadAllText(Path.Combine(content.RootDirectory, "robo/robo.json")));
        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, Robot gameHero)
        {
            foreach (Robot r in Enemies)
            {
                r.Update(gameTime, gameCamera, gameMap, levelSectors, walkableLayers, gameHero);
            }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera, float minY, float maxY)
        {
            foreach (Robot r in Enemies.OrderBy(rob=>rob.landingHeight))
            {
                if(r.Position.Y>=minY && r.Position.Y<maxY)
                    r.Draw(gd,sb,gameCamera);
            }
        }

        public int Spawn(Map gameMap, Camera gameCamera, List<int> levelSectors)
        {
            int numSpawned = 0;
            // Left or right side?
            int side = rand.Next(2);
            if (side == 0) side = -1;

            // Actual X spawn position
            Vector2 spawnPos = new Vector2(gameCamera.Position.X + (((gameCamera.Width / 2) - 100f) * side), gameCamera.Position.Y - (gameCamera.Width / 2));

            // Detect a Y position
            bool spawned = false;
            for (float y = spawnPos.Y; y < spawnPos.Y + gameCamera.Height; y += 20f)
            {
                if (!spawned)
                {
                    for (int i = 0; i < levelSectors.Count; i++)
                    {
                        MapObjectLayer walkableLayer = gameMap.GetLayer("Walkable" + levelSectors[i].ToString()) as MapObjectLayer;
                        foreach (MapObject o in walkableLayer.Objects)
                        {
                            if (!spawned && Helper.IsPointInShape(new Vector2(spawnPos.X - ((gameMap.Width * gameMap.TileWidth) * i), y), o.LinePoints))
                            {
                                if (rand.Next(5) == 1)
                                {
                                    numSpawned++;
                                    spawned = true;

                                    Robot r = new Robot(new Vector2(spawnPos.X,y), false);
                                    r.LoadContent(skeletonRenderer, blankTex, AtlasDict["robo"], JsonDict["robo"]);
                                    Enemies.Add(r);
                                }
                            }
                        }
                    }
                }
            }

            return numSpawned;
        }
    }
}
