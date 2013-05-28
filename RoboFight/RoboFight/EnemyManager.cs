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

        public static EnemyManager Instance;

        Texture2D blankTex;

        SkeletonRenderer skeletonRenderer;

        public Dictionary<string, Atlas> AtlasDict = new Dictionary<string, Atlas>();
        Dictionary<string, string> JsonDict = new Dictionary<string, string>();

        static Random rand = new Random();

        int largestNumberSpawned = 0;

        public EnemyManager()
        {
            Instance = this;
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            blankTex = content.Load<Texture2D>("blank");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);

            AtlasDict.Add("robo", new Atlas(graphicsDevice, "robo/robo.atlas", content));
            JsonDict.Add("robo", File.ReadAllText(Path.Combine(content.RootDirectory, "robo/robo.json")));
        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, Robot gameHero)
        {
            foreach (Robot r in Enemies)
            {
                r.Update(gameTime, gameCamera, gameMap, levelSectors, walkableLayers, gameHero);
                r.Position = Vector2.Clamp(r.Position, gameCamera.Position - (new Vector2(gameCamera.Width + 300f, gameCamera.Height) / 2), gameCamera.Position + (new Vector2(gameCamera.Width + 300f, gameCamera.Height) / 2));
            }

            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i].Dead) Enemies.RemoveAt(i);
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

        public int Spawn(Map gameMap, Camera gameCamera, List<int> levelSectors, Robot gameHero)
        {
            int numSpawned = 0;
            // Left or right side?

            for (int num = 0; num < 1 + rand.Next(gameHero.Sector); num++)
            {
                if (numSpawned > largestNumberSpawned) break;

                int side = rand.Next(2);
                if (side == 0) side = -1;

                // Actual X spawn position
                Vector2 spawnPos = new Vector2(gameCamera.Position.X + (((gameCamera.Width / 2) + 50f + ((float)rand.NextDouble() * 100f)) * side), gameCamera.Position.Y - (gameCamera.Width / 2));

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
                                    if (rand.Next(3) == 1)
                                    {
                                        numSpawned++;
                                        spawned = true;

                                        Robot r = new Robot(new Vector2(spawnPos.X, y), false);
                                        if (rand.Next(5) == 0) ItemManager.Instance.Spawn(r);
                                        r.LoadContent(skeletonRenderer, blankTex, AtlasDict["robo"], JsonDict["robo"]);
                                        Enemies.Add(r);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (numSpawned > largestNumberSpawned) largestNumberSpawned = numSpawned;

            return numSpawned;
        }

        public bool CheckAttack(Vector2 pos, int faceDir, float power, float maxDist, int maxHits)
        {
            float mindist = 10000f;
            Robot target = null;
            int numHits = 0;

            foreach (Robot r in Enemies)
            {
                if ((r.Position - pos).Length() < mindist && (r.Position - pos).Length()<maxDist && r.Active)
                {
                    if ((faceDir == 1 && r.Position.X > pos.X) || (faceDir == -1 && r.Position.X < pos.X))
                    {
                        if (r.Position.Y > pos.Y - 30f && r.Position.Y < pos.Y + 30f)
                        {
                            numHits++;
                            if(numHits<=maxHits)
                                r.DoHit(pos, power);
                            mindist = (r.Position - pos).Length();
                        }
                    }
                }
            }

            return (numHits > 0);
        }

        
    }
}
