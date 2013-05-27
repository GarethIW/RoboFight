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
    public class Robot
    {
        static Random rand = new Random();

        public bool IsPlayer = false;

        public Vector2 Position;
        public Vector2 Speed;
        public Vector2 JumpSpeed;

        public int Sector = 0;

        public float landingHeight;

        public float Scale = 1f;

        public float Health = 100f;
        public bool Active = true;
        public bool Dead = false;

        public Item Item;

        Vector2 gravity = new Vector2(0f, 0.333f);

        Rectangle collisionRect = new Rectangle(0, 0, 85, 150);

        Texture2D blankTex;

        SkeletonRenderer skeletonRenderer;
        Skeleton skeleton;
        Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();

        float animTime;

        public int faceDir = 1;

        bool walking = false;
        bool jumping = false;
        bool falling = false;

        bool punchHeld = false;
        bool punchReleased = false;
        double punchReleaseTime = 0;
        float punchAnimTime = 0;

        Vector2 spawnPosition;

        Color tint = Color.White;

        bool pushingUp = false;

        float attackCharge = 0f;

        double knockbackTime = 0;
        double deadTime = 0;
        float alpha = 1f;

        bool pickingUp = false;
        bool hasPickedUp = false;
        double pickupTime = 0;

        // AI stuff
        Vector2 targetPosition = Vector2.Zero;
        bool backingOff = false;
        double notMovedTime = 0;

        public Robot(Vector2 spawnpos, bool isPlayer)
        {
            IsPlayer = isPlayer;
            spawnPosition = spawnpos;

            Position = spawnPosition;
        }
        public Robot(Vector2 spawnpos, bool isPlayer, Robot gameHero)
        {
            IsPlayer = isPlayer;
            spawnPosition = spawnpos;
            
            Position = spawnPosition;
            if (Position.X > gameHero.Position.X) faceDir = -1; else faceDir = 1;
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
            Atlas atlas = new Atlas(graphicsDevice, "robo/robo.atlas", content);
            SkeletonJson json = new SkeletonJson(atlas);
            skeleton = new Skeleton(json.readSkeletonData("robo", File.ReadAllText(Path.Combine(content.RootDirectory, "robo/robo.json"))));
            skeleton.SetSkin("default");
            skeleton.SetSlotsToBindPose();

            
            //skeleton.FindSlot("melee-item").Attachment = itemAttach;
            skeleton.SetAttachment("melee-item", "crowbar");
            

            Animations.Add("walk", skeleton.Data.FindAnimation("walk"));
            Animations.Add("punch-hold", skeleton.Data.FindAnimation("punch-hold"));
            Animations.Add("punch-release", skeleton.Data.FindAnimation("punch-release"));
            Animations.Add("knockback", skeleton.Data.FindAnimation("knockback"));
            Animations.Add("pickup", skeleton.Data.FindAnimation("pickup"));
            Animations.Add("knockout", skeleton.Data.FindAnimation("knockout"));

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;
            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            skeleton.UpdateWorldTransform();


            ItemManager.Instance.Spawn(this);
        }

        public void LoadContent(SkeletonRenderer sr, Texture2D bt, Atlas atlas, string json)
        {
            blankTex = bt;
            skeletonRenderer =sr;

            SkeletonJson skjson = new SkeletonJson(atlas);
            skeleton = new Skeleton(skjson.readSkeletonData("robo", json));

            tint = new Color(0.5f + ((float)rand.NextDouble() * 0.5f), 0.5f + ((float)rand.NextDouble() * 0.5f), 0.5f + ((float)rand.NextDouble() * 0.5f));
            skeleton.R = tint.ToVector3().X;
            skeleton.G = tint.ToVector3().Y;
            skeleton.B = tint.ToVector3().Z;

            foreach (Slot s in skeleton.Slots)
            {
                s.Data.R = skeleton.R;
                s.Data.G = skeleton.G;
                s.Data.B = skeleton.B;
            }

            skeleton.SetSkin("default");
            skeleton.SetSlotsToBindPose();
            Animations.Add("walk", skeleton.Data.FindAnimation("walk"));
            Animations.Add("punch-hold", skeleton.Data.FindAnimation("punch-hold"));
            Animations.Add("punch-release", skeleton.Data.FindAnimation("punch-release"));
            Animations.Add("knockback", skeleton.Data.FindAnimation("knockback"));
            Animations.Add("pickup", skeleton.Data.FindAnimation("pickup"));
            Animations.Add("knockout", skeleton.Data.FindAnimation("knockout"));

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;
            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            skeleton.UpdateWorldTransform();


        }

        public void Update(GameTime gameTime, Camera gameCamera, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, Robot gameHero)
        {
            if (Active)
            {
                if (!IsPlayer)
                {
                    if (notMovedTime > 500)
                    {
                        if (CheckJump(gameMap, levelSectors, walkableLayers))
                        {
                            notMovedTime = 0;
                            Jump();
                        }
                    }

                    if (notMovedTime > 1000)
                    {
                        backingOff = true;
                        targetPosition = MoveToRandomPosition(gameMap, levelSectors, walkableLayers);
                    }

                    if (!backingOff)
                    {
                        targetPosition.X = gameHero.Position.X;
                        targetPosition.Y = gameHero.landingHeight;
                    }
                    else
                        if (rand.Next(250) == 1) backingOff = false;

                    if ((new Vector2(Position.X, landingHeight) - targetPosition).Length() < 100f)
                    {
                        if (rand.Next(100) == 1)
                        {
                            backingOff = true;
                            targetPosition.X = (gameHero.Position.X - 300f) + (600f * (float)rand.NextDouble());
                            targetPosition.Y = (gameHero.landingHeight - 100f) + (200f * (float)rand.NextDouble());
                        }
                    }

                    if (targetPosition.X - 50 > Position.X)
                        MoveLeftRight(1);

                    if (targetPosition.X + 50 < Position.X)
                        MoveLeftRight(-1);

                    if (targetPosition.Y - landingHeight < -5 || targetPosition.Y - landingHeight > 5)
                    {
                        if (targetPosition.Y > landingHeight)
                            MoveUpDown(1);

                        if (targetPosition.Y < landingHeight)
                            MoveUpDown(-1);
                    }


                    if (gameHero.Position.X > Position.X) faceDir = 1; else faceDir = -1;
                }

                if (!walking && !jumping && knockbackTime <= 0)
                {
                    Animations["walk"].Apply(skeleton, 0f, false);
                }

                if (walking && !jumping && knockbackTime <= 0)
                {
                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 2;

                    Animations["walk"].Mix(skeleton, animTime, true, 0.3f);
                }

                if (pickingUp)
                {
                    pickupTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;

                    Animations["pickup"].Apply(skeleton, animTime, false);

                    if (pickupTime > 150 && !hasPickedUp)
                    {
                        ItemManager.Instance.AttemptPickup(this);
                        hasPickedUp = true;
                    }
                    if (pickupTime >= 300)
                    {
                        pickingUp = false;
                        hasPickedUp = false;
                    }
                }

                if (knockbackTime > 0)
                {
                    knockbackTime -= gameTime.ElapsedGameTime.TotalMilliseconds;

                    animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f) * 3;
                    Animations["knockback"].Mix(skeleton, animTime, true, 0.3f);

                    CheckCollision(gameTime, gameMap, levelSectors, walkableLayers);
                    Position += (Speed);

                    if (Speed.X > 0) Speed.X -= 0.1f;
                    else if (Speed.X < 0) Speed.X += 0.1f;

                    if (Speed.X > -0.1f && Speed.X < 0.1f) knockbackTime = 0;
                }
                else
                {
                    

                    if (jumping)
                    {
                        Position += JumpSpeed;
                        JumpSpeed += gravity;
                        if (JumpSpeed.Y > 0f) { jumping = false; falling = true; }

                        animTime += gameTime.ElapsedGameTime.Milliseconds / 1000f;
                        //Animations["jump"].Mix(skeleton, animTime, false, 0.5f);
                    }

                    if (!jumping && !falling) landingHeight = Position.Y;

                    if (punchHeld)
                    {
                        attackCharge += 0.25f;
                        Animations["punch-hold"].Apply(skeleton, 1f, false);
                    }
                    else if (punchReleased)
                    {
                        if (punchReleaseTime == 0)
                        {
                            if (IsPlayer)
                                EnemyManager.Instance.CheckAttack(Position, faceDir, attackCharge, 100f);
                            else
                                if ((Position - gameHero.Position).Length() < 100f) gameHero.DoHit(Position, attackCharge);
                        }

                        punchReleaseTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                        if (punchReleaseTime >= 200)
                        {
                            punchReleaseTime = 0;
                            punchReleased = false;
                            Animations["punch-release"].Apply(skeleton, 0f, false);
                        }

                        Animations["punch-release"].Apply(skeleton, 1f, false);

                        attackCharge = 0f;
                    }

                    attackCharge = MathHelper.Clamp(attackCharge, 0f, 50f);

                    Speed.Normalize();

                    if (Speed.Length() > 0f || pushingUp)
                    {
                        CheckCollision(gameTime, gameMap, levelSectors, walkableLayers);
                        if (Speed.Length() > 0f)
                            Position += (Speed * 4f);
                    }

                    Speed = Vector2.Zero;
                }

                if (Item != null)
                {
                    if (Item.Type == ItemType.Melee)
                    {
                        skeleton.SetAttachment("melee-item", Item.Name);
                        skeleton.SetAttachment("projectile-item", null);
                    }
                    else
                    {
                        skeleton.SetAttachment("projectile-item", Item.Name);
                        skeleton.SetAttachment("melee-item", null);
                    }
                }
                else
                {
                    skeleton.SetAttachment("melee-item", null);
                    skeleton.SetAttachment("projectile-item", null);
                }

                pushingUp = false;
            }

            if (Health <= 0)
            {
                Active = false;

                animTime += (gameTime.ElapsedGameTime.Milliseconds / 1000f);
                Animations["knockout"].Mix(skeleton, animTime, true, 0.2f);

                deadTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (deadTime > 0 && deadTime<1000)
                {
                    CheckCollision(gameTime, gameMap, levelSectors, walkableLayers);
                    Position.X += ((float)-faceDir) * (0.001f * (1000f - (float)deadTime));
                }
                if (deadTime > 2000 && alpha > 0f)
                {
                    alpha -= 0.025f;
                    alpha = MathHelper.Clamp(alpha, 0f, 1f);
                }

                

                if (deadTime >= 3000)
                {
                    Dead = true;
                }
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

            skeleton.A = alpha;
            foreach (Slot s in skeleton.Slots)
            {
                s.A = skeleton.A;
            }

            skeleton.RootBone.ScaleX = Scale;
            skeleton.RootBone.ScaleY = Scale;

            collisionRect.Location = new Point((int)Position.X - (collisionRect.Width / 2), (int)Position.Y - (collisionRect.Height));

            Position.X = MathHelper.Clamp(Position.X, 0, (gameMap.Width * gameMap.TileWidth) * levelSectors.Count);
            Position.Y = MathHelper.Clamp(Position.Y, 0, gameMap.Height * gameMap.TileHeight);

            skeleton.RootBone.X = Position.X;
            skeleton.RootBone.Y = Position.Y;

            if (faceDir == -1) skeleton.FlipX = true; else skeleton.FlipX = false;

            skeleton.UpdateWorldTransform();

            walking = false;

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
            if (knockbackTime > 0 || pickingUp) return;

            if (dir > 0) faceDir = 1; else faceDir = -1;

            Speed.X = dir;
            walking = true;

        }

        public void MoveUpDown(float dir)
        {
            if (knockbackTime > 0 || pickingUp) return;

            if(dir==-1) pushingUp = true;
            if (jumping || falling) return;
           // if (dir > 0) faceDir = 1; else faceDir = -1;

            Speed.Y = dir;
            walking = true;

        }

        public void Jump()
        {
            if (knockbackTime > 0 || pickingUp) return;

            if (!jumping && !falling)
            {
               
                jumping = true;
                animTime = 0;
              
                JumpSpeed.Y = -12f;
            }
        }

        public void Pickup()
        {
            if (knockbackTime > 0 || pickingUp || jumping || falling) return;

            animTime = 0;
            pickingUp = true;
            pickupTime = 0;
        }

        public void Attack(bool p)
        {
            if (knockbackTime > 0 || pickingUp) return;

            if (punchHeld && !p) punchReleased = true;
            punchHeld = p;
        }


        void CheckCollision(GameTime gameTime, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            if ((jumping || falling))
            {
                float originalLandingHeight = landingHeight;
                bool found = false;

                for (landingHeight = originalLandingHeight; landingHeight >= Position.Y; landingHeight--)
                {
                    if (Speed.X < 0 && !CheckCollisionLeft(gameMap, levelSectors, walkableLayers)) { found = true; break; }
                    if (Speed.X > 0 && !CheckCollisionRight(gameMap, levelSectors, walkableLayers)) { found = true; break; }
                    if (pushingUp && !CheckCollisionUp(gameMap, levelSectors, walkableLayers)) { found = true; break; }
                }
                if (!found) landingHeight = originalLandingHeight;
            }

            if (Speed.X > 0f)
            {
                if (CheckCollisionRight(gameMap, levelSectors, walkableLayers))
                {
                    if (!FallTest(gameMap, levelSectors, walkableLayers))
                    {
                        Speed.X = 0f;
                        notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                    else notMovedTime = 0;
                }
                else notMovedTime = 0;
            }
            if (Speed.X < 0f)
            {
                if (CheckCollisionLeft(gameMap, levelSectors, walkableLayers))
                {
                    if (!FallTest(gameMap, levelSectors, walkableLayers))
                    {
                        Speed.X = 0f;
                        notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                    else notMovedTime = 0;
                }
                else notMovedTime = 0;
            }

            if (Speed.Y > 0f)
            {
                if (CheckCollisionDown(gameMap, levelSectors, walkableLayers))
                {
                    if (!FallTest(gameMap, levelSectors, walkableLayers))
                    {
                        Speed.Y = 0f;
                        notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    }
                    else notMovedTime = 0;
                }
                else notMovedTime = 0;
            }
            if (Speed.Y < 0f || ((jumping || falling) && pushingUp))
            {
                if (CheckCollisionUp(gameMap, levelSectors, walkableLayers))
                {
                    //if (!FallTest(gameMap, levelSectors, walkableLayers))
                    //{
                        Speed.Y = 0f;
                        notMovedTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                    //}
                    //else notMovedTime = 0;
                }
                else notMovedTime = 0;
            }

           

        }

        bool FallTest(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                for (int o = 0; o < walkableLayer.Objects.Count;o++)
                {
                    for (float y = landingHeight + 20; y < (gameMap.TileHeight * (gameMap.Height-5)); y+=5)
                    {

                        if (Helper.IsPointInShape(new Vector2((Position.X + (Speed.X * 10)) - ((gameMap.Width * gameMap.TileWidth) * i), y), walkableLayer.Objects[o].LinePoints))
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

        bool CheckCollisionRight(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                for (int o = 0; o < walkableLayer.Objects.Count; o++)
                {
                    for(int x=1;x<10;x+=3)
                        if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(x, 0), walkableLayer.Objects[o].LinePoints)) return false;
                }
            }

            return true;
        }
        bool CheckCollisionLeft(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                for (int o = 0; o < walkableLayer.Objects.Count; o++)
                {
                    for (int x = 1; x < 10; x+=3)
                        if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(-x, 0), walkableLayer.Objects[o].LinePoints)) return false;
                }
            }

            return true;
        }
        bool CheckCollisionUp(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                for (int o = 0; o < walkableLayer.Objects.Count;o++)
                {
                    if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(0, -10), walkableLayer.Objects[o].LinePoints)) return false;
                }
            }

            return true;
        }
        bool CheckCollisionDown(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                for (int o = 0; o < walkableLayer.Objects.Count; o++)
                {
                    if (Helper.IsPointInShape(new Vector2(Position.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight) + new Vector2(0, 10), walkableLayer.Objects[o].LinePoints)) return false;
                }
            }

            return true;
        }

        Vector2 MoveToRandomPosition(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                foreach (MapObject o in walkableLayer.Objects)
                {
                    if (Helper.IsPointInShape(new Vector2((Position.X + 5f) - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), o.LinePoints) ||
                        Helper.IsPointInShape(new Vector2((Position.X - 5f) - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), o.LinePoints) ||
                        Helper.IsPointInShape(new Vector2((Position.X) - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), o.LinePoints))
                    {
                        int lx = 100000;
                        int ly = 100000;
                        int mx = -100000;
                        int my = -100000;
                        foreach (Point l in o.LinePoints)
                        {
                            if (l.X < lx) lx = l.X;
                            if (l.X > mx) mx = l.X;
                            if (l.Y < ly) ly = l.Y;
                            if (l.Y > my) my = l.Y;
                        }

                        Vector2 testPoint = new Vector2(lx + (rand.Next(mx - lx)), ly + (rand.Next(my - ly)));
                        if (Helper.IsPointInShape(testPoint, o.LinePoints)) return testPoint + new Vector2(((gameMap.Width * gameMap.TileWidth) * i),0);
                    }
                }
            }

            return Position;
        }

        bool CheckJump(Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];


                for (float y = landingHeight - 20; y > landingHeight-300; y--)
                {

                    foreach (MapObject o in walkableLayer.Objects)
                    {
                        if (Helper.IsPointInShape(new Vector2((Position.X) - ((gameMap.Width * gameMap.TileWidth) * i), y), o.LinePoints))
                        {
                            
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        internal void DoHit(Vector2 pos, float power)
        {
            if (power > 5f && knockbackTime <= 0)
            {
                knockbackTime = (double)((power * 100f) / 2f);
                Speed.X = 10f * -(float)faceDir;
            }
            Health -= power;

            if (Health <= 0)
            {
                if (Item != null)
                {
                    Item.InWorld = true;
                    Item.Position = Position + new Vector2(faceDir * 75, -75);
                    Item.DroppedPosition = Position;
                    Item.Speed.Y = 2f;
                    Item = null;
                }
            }
        }
    }
}
