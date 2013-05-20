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
    class Hero
    {
        static Random rand = new Random();

        public Vector2 Position;
        public Vector2 Speed;
        public Vector2 JumpSpeed;

        public int Sector = 0;

        float landingHeight;

        public float Scale = 1f;

        Vector2 gravity = new Vector2(0f, 0.333f);

        Rectangle collisionRect = new Rectangle(0, 0, 85, 150);

        Texture2D blankTex;

        SkeletonRenderer skeletonRenderer;
        Skeleton skeleton;
        Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();

        float animTime;

        int faceDir = 1;

        bool walking = false;
        bool jumping = false;
        bool falling = false;

        Vector2 spawnPosition;

        public Hero(Vector2 spawnpos)
        {
            spawnPosition = spawnpos;

            Position = spawnPosition;
        }

        public void Reset()
        {
            faceDir = 1;

            walking = false;
            jumping = false;
            falling = false;

            Position = spawnPosition;

            Speed = Vector2.Zero;
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            blankTex = content.Load<Texture2D>("blank");

            skeletonRenderer = new SkeletonRenderer(graphicsDevice);
            Atlas atlas = new Atlas(graphicsDevice, Path.Combine(content.RootDirectory, "robo/robo.atlas"));
            SkeletonJson json = new SkeletonJson(atlas);
            skeleton = new Skeleton(json.readSkeletonData("robo", File.ReadAllText(Path.Combine(content.RootDirectory, "robo/robo.json"))));
            skeleton.SetSkin("default");
            skeleton.SetSlotsToBindPose();
            Animations.Add("walk", skeleton.Data.FindAnimation("walk"));
            //Animations.Add("jump", skeleton.Data.FindAnimation("jump"));

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;
            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            skeleton.UpdateWorldTransform();
        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, List<int> levelSectors)
        {
            if (!walking && !jumping)
            {
                skeleton.SetToBindPose();
            }

            if (walking && !jumping)
            {
                animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 2;
               
                    Animations["walk"].Mix(skeleton, animTime, true, 0.3f);
            }

            if (falling)
            {
                Position += JumpSpeed;
                JumpSpeed += gravity;

                if (Position.Y >= landingHeight)
                {
                    falling = false;
                    JumpSpeed = Vector2.Zero;
                    Position.Y = landingHeight;
                }
            }

            if (jumping)
            {
                Position += JumpSpeed;
                JumpSpeed += gravity;
                if (JumpSpeed.Y > 0f) { jumping = false; falling = true; }

                animTime += gameTime.ElapsedGameTime.Milliseconds / 1000f;
                //Animations["jump"].Mix(skeleton, animTime, false, 0.5f);
            }

            if (!jumping && !falling) landingHeight = Position.Y;

            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            Speed.Normalize();

            if (Speed.Length() > 0f)
            {
                CheckCollision(gameMap, levelSectors);
                Position += (Speed * 4f);
            }

            collisionRect.Location = new Point((int)Position.X - (collisionRect.Width / 2), (int)Position.Y - (collisionRect.Height));
            

            Position.X = MathHelper.Clamp(Position.X, 0, (gameMap.Width * gameMap.TileWidth) * levelSectors.Count);
            Position.Y = MathHelper.Clamp(Position.Y, 0, gameMap.Height * gameMap.TileHeight);

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;


            if (faceDir == -1) skeleton.FlipX = true; else skeleton.FlipX = false;

            skeleton.UpdateWorldTransform();

            walking = false;
            Speed = Vector2.Zero;


            if (Position.X > (gameMap.Width * gameMap.TileWidth) * (Sector + 1))
                Sector++;
            if (Position.X < (gameMap.Width * gameMap.TileWidth) * (Sector))
                Sector--;

        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera gameCamera)
        {
            skeletonRenderer.Begin(graphicsDevice, gameCamera.CameraMatrix);
            skeletonRenderer.Draw(skeleton);
            skeletonRenderer.End();

            // Draw collision box
            //spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
            //spriteBatch.Draw(blankTex, collisionRect, Color.White * 0.3f);
            //spriteBatch.End();
        }


        public void MoveLeftRight(float dir)
        {

            if (dir > 0) faceDir = 1; else faceDir = -1;

            Speed.X = dir;
            walking = true;

        }

        public void MoveUpDown(float dir)
        {
            if (jumping || falling) return;
           // if (dir > 0) faceDir = 1; else faceDir = -1;

            Speed.Y = dir;
            walking = true;

        }

        public void Jump()
        {

            if (!jumping && !falling)
            {
               
                jumping = true;
                animTime = 0;
              
                JumpSpeed.Y = -12f;
            }
        }

        public void Crouch()
        {
           
        }

        void CheckCollision(Map gameMap, List<int> levelSectors)
        {
            

            

            if (jumping || falling && Speed.X>-0.1f && Speed.X<0.1f)
            {
                float originalLandingHeight = landingHeight;
                bool found = false;

                for (landingHeight = originalLandingHeight; landingHeight >= Position.Y; landingHeight--)
                {
                    if (Speed.X < 0 && !CheckCollisionLeft(gameMap, levelSectors)) { found = true; break; }
                    if (Speed.X > 0 && !CheckCollisionRight(gameMap, levelSectors)) { found = true; break; }
                }
                if (!found) landingHeight = originalLandingHeight;
            }

            if (Speed.X > 0f)
            {
                if (CheckCollisionRight(gameMap, levelSectors))
                {
                    if (!FallTest(gameMap, levelSectors))
                    {
                        Speed.X = 0f;
                    }

                    //Position.X -= 4f;
                }
            }
            if (Speed.X < 0f)
            {
                if (CheckCollisionLeft(gameMap, levelSectors))
                {
                    if (!FallTest(gameMap, levelSectors))
                    {
                        Speed.X = 0f;
                    }
                    //Position.X += 4f;
                }
            }

            if (Speed.Y > 0f)
            {
                if (CheckCollisionDown(gameMap, levelSectors))
                {
                    if (!FallTest(gameMap, levelSectors))
                    {
                        Speed.Y = 0f;
                    }
                    //Position.Y -= 4f;
                }
            }
            if (Speed.Y < 0f)
            {
                if (CheckCollisionUp(gameMap, levelSectors))
                {
                    Speed.Y = 0f;
                    //Position.Y += 4f;
                }
            }

            //collisionRect.Offset(new Point((int)Speed.X, (int)Speed.Y));

            //Rectangle? collRect;

            //// Check for ledge grabs
            //if ((jumping || falling) && !justUngrabbed)
            //{
            //    if (Speed.X < 0 && gameMap.CheckTileCollision(new Vector2(collisionRect.Left, collisionRect.Top), Layer))
            //        if (!gameMap.CheckTileCollision(new Vector2(collisionRect.Left, collisionRect.Top - gameMap.TileHeight), Layer) &&
            //           !gameMap.CheckTileCollision(new Vector2(collisionRect.Left + gameMap.TileWidth, collisionRect.Top - gameMap.TileHeight), Layer) &&
            //           !gameMap.CheckTileCollision(new Vector2(collisionRect.Left + gameMap.TileWidth, collisionRect.Top), Layer))
            //        {
            //            grabbed = true;
            //            jumping = false;
            //            falling = false;
            //            crouching = false;
            //            Speed.Y = 0;
            //            Speed.X = 0;
            //            grabbedPosition = new Vector2((int)(collisionRect.Left / gameMap.TileWidth) * gameMap.TileWidth, (int)(collisionRect.Top / gameMap.TileHeight) * gameMap.TileHeight) + new Vector2(gameMap.TileWidth, collisionRect.Height + 30);
            //            faceDir = -1;
            //        }

            //    if (Speed.X > 0 && gameMap.CheckTileCollision(new Vector2(collisionRect.Right, collisionRect.Top), Layer))
            //        if (!gameMap.CheckTileCollision(new Vector2(collisionRect.Right, collisionRect.Top - gameMap.TileHeight), Layer) &&
            //           !gameMap.CheckTileCollision(new Vector2(collisionRect.Right - gameMap.TileWidth, collisionRect.Top - gameMap.TileHeight), Layer) &&
            //           !gameMap.CheckTileCollision(new Vector2(collisionRect.Right - gameMap.TileWidth, collisionRect.Top), Layer))
            //        {
            //            grabbed = true;
            //            jumping = false;
            //            falling = false;
            //            crouching = false;
            //            Speed.Y = 0;
            //            Speed.X = 0;
            //            grabbedPosition = new Vector2((int)(collisionRect.Right / gameMap.TileWidth) * gameMap.TileWidth, (int)(collisionRect.Top / gameMap.TileHeight) * gameMap.TileHeight) + new Vector2(0, collisionRect.Height + 30);
            //            faceDir = 1;
            //        }
            //}

            //if (grabbed || climbing) return;



            //collRect = CheckCollisionBottom(gameMap);
            //if (collRect.HasValue)
            //{
            //    if (falling)
            //    {
            //        Speed.Y = 0f;
            //        Position.Y -= collRect.Value.Height;
            //        collisionRect.Offset(0, -collRect.Value.Height);
            //        jumping = false;
            //        falling = false;
            //        justUngrabbed = false;
            //        Tile t = gameMap.GetTile(Position + new Vector2(0, 30), Layer);
            //        if (t != null)
            //        {
            //            if (t.Properties.Contains("Wood"))
            //                AudioController.PlaySFX("fstep-wood", -0.2f, 0.2f);
            //            else if (t.Properties.Contains("Metal"))
            //                AudioController.PlaySFX("fstep-metal", -0.2f, 0.2f);
            //            else AudioController.PlaySFX("fstep-grass", -0.2f, 0.2f);
            //        }
            //        else AudioController.PlaySFX("fstep-grass", -0.2f, 0.2f);
            //    }

            //}
            //else
            //    falling = true;

            //if (Speed.Y < 0f)
            //{
            //    collRect = CheckCollisionTop(gameMap);
            //    if (collRect.HasValue)
            //    {
            //        Speed.Y = 0f;
            //        Position.Y += collRect.Value.Height;
            //        collisionRect.Offset(justUngrabbed ? (collRect.Value.Width * (-faceDir)) : 0, collRect.Value.Height);
            //        falling = true;
            //        jumping = false;
            //    }
            //}




            //if (Speed.X > 0f)
            //{
            //    collRect = CheckCollisionRight(gameMap);
            //    if (collRect.HasValue)
            //    {
            //        Speed.X = 0f;
            //        Position.X -= (collRect.Value.Width);
            //        collisionRect.Offset(-collRect.Value.Width, 0);
            //    }
            //}
            //if (Speed.X < 0f)
            //{
            //    collRect = CheckCollisionLeft(gameMap);
            //    if (collRect.HasValue)
            //    {
            //        Speed.X = 0f;
            //        Position.X += collRect.Value.Width;
            //        collisionRect.Offset(collRect.Value.Width, 0);
            //    }
            //}


            //bool collided = false;
            //for (int y = -1; y > -15; y--)
            //{
            //    collisionRect.Offset(0, -1);
            //    collRect = CheckCollisionTop(gameMap);
            //    if (collRect.HasValue) collided = true;
            //}
            //if (!collided) crouching = false;

        }

        bool FallTest(Map gameMap, List<int>levelSectors)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = gameMap.GetLayer("Walkable" + levelSectors[i].ToString()) as MapObjectLayer;


                for (float y = landingHeight + 20; y < (gameMap.TileHeight * gameMap.Height); y++)
                {

                    foreach (MapObject o in walkableLayer.Objects)
                    {
                        if (Helper.IsPointInShape(new Vector2(Position.X-((gameMap.Width * gameMap.TileWidth) * i), y), o.LinePoints))
                        {
                            if ((y - landingHeight) > gameMap.TileHeight)
                            {
                                landingHeight = y;
                                falling = true;
                                return true;
                            }
                            else return false;
                        }
                    }
                }
            }

            return false;
        }

        bool CheckCollisionRight(Map gameMap, List<int> levelSectors)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = gameMap.GetLayer("Walkable" + levelSectors[i].ToString()) as MapObjectLayer;
                foreach (MapObject o in walkableLayer.Objects)
                {
                    if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(10, 0), o.LinePoints)) return false;
                }
            }

            return true;
        }
        bool CheckCollisionLeft(Map gameMap, List<int> levelSectors)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = gameMap.GetLayer("Walkable" + levelSectors[i].ToString()) as MapObjectLayer;
                foreach (MapObject o in walkableLayer.Objects)
                {
                    if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(-10, 0), o.LinePoints)) return false;
                }
            }

            return true;
        }
        bool CheckCollisionUp(Map gameMap, List<int> levelSectors)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = gameMap.GetLayer("Walkable" + levelSectors[i].ToString()) as MapObjectLayer;
                foreach (MapObject o in walkableLayer.Objects)
                {
                    if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(0, -10), o.LinePoints)) return false;
                }
            }

            return true;
        }
        bool CheckCollisionDown(Map gameMap, List<int> levelSectors)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = gameMap.GetLayer("Walkable" + levelSectors[i].ToString()) as MapObjectLayer;
                foreach (MapObject o in walkableLayer.Objects)
                {
                    if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(0, 10), o.LinePoints)) return false;
                }
            }

            return true;
        }
    }
}
