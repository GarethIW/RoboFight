using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;

namespace RoboFight
{
    public enum ProjectileType
    {
        Laser
    }

    public class Projectile
    {
        public Vector2 Position;
        public Vector2 Speed;
        public bool OwnedByHero;
        public ProjectileType Type;
        public bool Active;
        public double Life;
        public float landingHeight;
        public float Power;

        public float alpha;
        public float rot;

        public void Spawn(Vector2 pos, Vector2 speed, double life, bool heroowner, ProjectileType type, float pow)
        {
            Active = true;
            Position = pos;
            Speed = speed;
            Life = life;
            OwnedByHero = heroowner;
            Type = type;
            Power = pow;

            alpha = 1f;
            rot = Helper.V2ToAngle(Speed);
        }
    }

    public class ProjectileManager
    {
        public static ProjectileManager Instance;

        const int MAX_PROJECTILES = 500;

        public static Random randomNumber = new Random();
        
        public List<Projectile> Projectiles = new List<Projectile>();

        public Texture2D spriteSheet;

        Vector2 frameSize = new Vector2(32, 32);

        public ProjectileManager()
        {
            Initialize();
        }

        public void Initialize()
        {
            Instance = this;
            for (int i = 0; i < MAX_PROJECTILES; i++)
                Projectiles.Add(new Projectile());
        }

        public void LoadContent(ContentManager content)
        {
            spriteSheet = content.Load<Texture2D>("projectiles");
            
        }

        public void Add(Vector2 loc, Vector2 speed, double life, bool ownerhero, ProjectileType type, float pow)
        {
            foreach (Projectile p in Projectiles)
            {
                if (!p.Active)
                {
                    p.Spawn(loc, speed, life, ownerhero, type, pow);
                    break;
                }
            }
        }

        public void Update(GameTime gameTime, Robot gameHero)
        {
            for (int p = 0; p < Projectiles.Count; p++)
            {
                if (Projectiles[p].Active)
                {
                    Projectiles[p].Position += Projectiles[p].Speed;

                    Projectiles[p].rot = Helper.V2ToAngle(Projectiles[p].Speed);


                    Projectiles[p].Life -= gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (Projectiles[p].Life <= 0)
                    {
                        //if (Projectiles[p].Type == ProjectileType.Grenade || Projectiles[p].Type == ProjectileType.Rocket) ExplodeGrenade(Projectiles[p], false);

                        Projectiles[p].alpha -= 0.1f;
                        if (Projectiles[p].alpha <= 0f) Projectiles[p].Active = false;
                    }

                    if (Projectiles[p].Life > 0 || Projectiles[p].alpha > 0)
                    {
                        //if (Projectiles[p].Type == ProjectileType.Grenade)
                        //{
                        //    Projectiles[p].Speed.Y += 0.1f;
                        //    //GameManager.ParticleController.Add(Projectiles[p].Position + new Vector2(((float)randomNumber.NextDouble() * 5f) - 2.5f, ((float)randomNumber.NextDouble() * 5f) - 2.5f), Vector2.Zero, 100f, false, false, new Rectangle(8,0,8,8), 0f, Color.Gray);

                        //    for (int i = 0; i < Projectiles.Count; i++)
                        //        if (Projectiles[i].OwnedByHero && Projectiles[i].Active)
                        //            if ((Projectiles[i].Position - Projectiles[p].Position).Length() <= 8) ExplodeGrenade(Projectiles[p], true);
                        //}

                              

                        // do collision checks
                        if (Projectiles[p].OwnedByHero)
                        {
                            foreach (Robot e in EnemyManager.Instance.Enemies)
                            {
                                if (!Projectiles[p].Active) break;

                                if (Projectiles[p].Position.X>e.Position.X-40 && Projectiles[p].Position.X<e.Position.X+40 &&
                                    Projectiles[p].Position.Y>e.Position.Y-150 && Projectiles[p].Position.Y<e.Position.Y &&
                                    Projectiles[p].landingHeight>e.landingHeight-30 && Projectiles[p].landingHeight<e.landingHeight+30)
                                {
                                    e.DoHit(Projectiles[p].Position, Projectiles[p].Power, Projectiles[p].Speed.X > 0f ? 1 : -1);
                                    Projectiles[p].Active = false;
                                }
                            }
                        }
                        else
                        {
                            if (Projectiles[p].Position.X > gameHero.Position.X - 40 && Projectiles[p].Position.X < gameHero.Position.X + 40 &&
                                    Projectiles[p].Position.Y > gameHero.Position.Y - 150 && Projectiles[p].Position.Y < gameHero.Position.Y &&
                                    Projectiles[p].landingHeight > gameHero.landingHeight - 30 && Projectiles[p].landingHeight < gameHero.landingHeight + 30)
                            {
                                gameHero.DoHit(Projectiles[p].Position, Projectiles[p].Power, Projectiles[p].Speed.X > 0f ? 1 : -1);
                                Projectiles[p].Active = false;
                            }
                                    
                        }
                    }
                }
            }
                    
           
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera, float minY, float maxY)
        {
            sb.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
            foreach (Projectile p in Projectiles.OrderBy(pro => pro.landingHeight))
            {
                if (p.Active && p.landingHeight >= minY && p.landingHeight < maxY)
                {
                    sb.Draw(spriteSheet, p.Position, new Rectangle((int)p.Type * (int)frameSize.X, 0, (int)frameSize.X, (int)frameSize.Y), Color.White * p.alpha, p.rot, frameSize / 2, 1f, SpriteEffects.None, 1);
                }
            }
            sb.End();
        }

        //void ExplodeGrenade(Projectile p, bool hurtsEnemies)
        //{
        //    p.Active = false;
        //    p.alpha = 0;
        //    p.Life = 0;

        //    for (float r = 0; r < 200; r += 20f)
        //    {
        //        for (float circ = 0; circ < MathHelper.TwoPi; circ += 0.25f)
        //        {
        //            Vector2 checkPos = p.Position + Helper.AngleToVector(circ, r);

        //            Vector2 speed = (p.Position - checkPos);
        //            speed.Normalize();

        //            if(!hurtsEnemies)
        //                GameManager.Hero.CheckHit(checkPos, speed * 2f, ProjectileType.Grenade, true);

        //            if(hurtsEnemies)
        //                foreach (Enemy e in GameManager.EnemyController.Enemies)
        //                    e.CheckHit(checkPos, speed);
        //         }
        //    }

        //    GameManager.ParticleController.AddExplosion(p.Position);
        //    AudioController.PlaySFX("explode", 0.9f, -0.5f, 0f, p.Position);

        //    if (hurtsEnemies)
        //        GameManager.HUD.AddScore(ScorePartType.Grenade);
        //}
    }
}
