﻿using Microsoft.Xna.Framework;
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
    class ItemManager
    {
        public List<Item> Items = new List<Item>();

        public static ItemManager Instance;

        SkeletonRenderer skeletonRenderer;

        Texture2D itemTex;
        Dictionary<string, Rectangle> sourceDict = new Dictionary<string,Rectangle>(); 

        static Random rand = new Random();

        public ItemManager()
        {
            Instance = this;
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            itemTex = content.Load<Texture2D>("robo/robo");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);

            sourceDict.Add("crowbar", new Rectangle(71, 63, 50, 20));
        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, Robot gameHero)
        {
            foreach (Item i in Items)
            {
                i.Update(gameTime);
            }

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Dead) Items.RemoveAt(i);
            }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera, float minY, float maxY)
        {
            sb.Begin(SpriteSortMode.Deferred, null,null,null,null,null,gameCamera.CameraMatrix);
            foreach (Item i in Items.OrderBy(it=>it.DroppedPosition.Y).Where(it=>it.InWorld))
            {
                if(i.Position.Y>=minY && i.Position.Y<maxY)
                    i.Draw(sb,gameCamera);
            }
            sb.End();
        }

        public void Spawn(Robot owner)
        {
            int item = rand.Next(1);

            Item newItem = null;

            switch (item)
            {
                case 0:
                    // crowbar
                    newItem = new Crowbar(itemTex, sourceDict["crowbar"]);
                    break;
            }

            newItem.Owner = owner;
            owner.Item = newItem;
            Items.Add(newItem);
        }

        internal void AttemptPickup(Robot robot)
        {
            if (robot.Item != null)
            {
                Item dropItem = robot.Item;
                robot.Item = null;
                dropItem.InWorld = true;
                dropItem.Position = robot.Position + new Vector2(robot.faceDir * 50, 0);
                dropItem.DroppedPosition = robot.Position;
            }
            else
            {
                foreach (Item i in Items)
                {
                    if (i.Position.X > robot.Position.X - 75f && i.Position.X < robot.Position.X + 75f)
                    {
                        if (i.Position.Y > robot.landingHeight - 30f && i.Position.Y < robot.landingHeight + 30f)
                        {
                            i.InWorld = false;
                            robot.Item = i;
                            break;
                        }
                    }
                }
            }
        }
    }
}
