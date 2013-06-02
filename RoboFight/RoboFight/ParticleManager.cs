using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using TiledLib;

namespace RoboFight
{
    public enum ParticleType
    {
        Standard,
        Health
    }

    public class Particle
    {
        public ParticleType Type;
        public Vector2 Position;
        public Vector2 Velocity;
        public float LandingHeight;
        public bool Active;
        public bool AffectedByGravity;
        public float Alpha;
        public double Life;
        public float RotationSpeed;
        public float Rotation;
        public Color Color;
        public Rectangle SourceRect;
    }

    public class ParticleManager
    {
        public static ParticleManager Instance;
        const int MAX_PARTICLES = 3000;

        public Particle[] Particles;
        public Random Rand = new Random();

        public Texture2D _texParticles;

        public ParticleManager()
        {
            Particles = new Particle[MAX_PARTICLES];
            Instance = this;
        }

        public void LoadContent(ContentManager content)
        {
            _texParticles = content.Load<Texture2D>("particles");

            for (int i = 0; i < MAX_PARTICLES; i++)
                Particles[i] = new Particle();
        }

        public void Update(GameTime gameTime, Robot gameHero, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers)
        {
            foreach (Particle p in Particles.Where(part => part.Active))
            {

                p.Life -= gameTime.ElapsedGameTime.TotalMilliseconds;

                if (p.Life > 0 || (p.Alpha>0f && p.Type!= ParticleType.Health))
                {
                    p.Position += p.Velocity;
                    p.Rotation += p.RotationSpeed;

                    if (p.AffectedByGravity) p.Velocity.Y += 0.2f;

                    if (p.Position.Y >= p.LandingHeight)
                    {
                        p.Velocity.Y = -(p.Velocity.Y / 2);
                        p.Velocity.X /= (2f + (float)Rand.NextDouble());
                        p.RotationSpeed = 0f;
                    }
                }

                if (p.AffectedByGravity)
                    if (CheckCollision(p.Position, p.LandingHeight, gameMap, levelSectors, walkableLayers, gameHero.Sector)) p.Velocity.X = 0f;

                if (p.Type != ParticleType.Health)
                {
                    if (p.Life <= 0)
                    {
                        p.Alpha -= 0.01f;
                        if (p.Alpha < 0.05f) p.Active = false;
                    }
                }

                switch (p.Type)
                {
                    case ParticleType.Health:
                        if ((p.Position - gameHero.Position).Length() < 200 && p.Velocity.Length()<1f && p.Life<=0 && !gameHero.Dead)
                        {
                            p.Position = Vector2.Lerp(p.Position, gameHero.Position + new Vector2(0, -75f), 0.1f);
                            if ((p.Position - (gameHero.Position + new Vector2(0, -75f))).Length() < 50f)
                            {
                                p.Alpha = 0f;
                                p.Active = false;
                                gameHero.Health += 1f;
                                if ((int)gameHero.Health == 121) gameHero.Score += 5;
                                gameHero.Health = MathHelper.Clamp(gameHero.Health, 0f, 121f);
                            }
                        }
                        break;
                }
            }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Camera gameCamera, float minY, float maxY)
        {
            sb.Begin(SpriteSortMode.Deferred, null, null, null, null, null, gameCamera.CameraMatrix);
            foreach (Particle p in Particles.Where(part=>part.Active).OrderBy(pro => pro.LandingHeight))
            {
                if (p.LandingHeight >= minY && p.LandingHeight < maxY)
                {
                    sb.Draw(_texParticles,
                            p.Position,
                            p.SourceRect, p.Color * p.Alpha, p.Rotation, new Vector2(p.SourceRect.Width / 2, p.SourceRect.Height / 2), 1f, SpriteEffects.None, 1);
                }
            }
            sb.End();
          
        }

        public void Add(ParticleType type, Vector2 spawnPos, float landingHeight, Vector2 velocity, float life, bool affectedbygravity, Rectangle sourcerect, float rot, Color col)
        {
            foreach (Particle p in Particles)
                if (!p.Active)
                {
                    p.Type = type;
                    p.Position = spawnPos;
                    p.LandingHeight = landingHeight;
                    p.Velocity = velocity;
                    p.Life = life;
                    p.AffectedByGravity = affectedbygravity;
                    p.SourceRect = sourcerect;
                    p.Alpha = 1f;
                    p.Active = true;
                    //p.RotationSpeed = rot;
                    p.Color = col;
                    p.Rotation = rot;
                    break;
                }
        }

        

        public void AddHurt(Vector2 pos, Vector2 velocity, float landingHeight, Color tint)
        {
            Vector2 tempV = pos;
            float amount = 0f;
            while (amount<1f)
            {
                Add(ParticleType.Standard, tempV, (landingHeight - 10f) + ((float)Rand.NextDouble() * 20f), velocity + new Vector2((float)(Rand.NextDouble()*2)-1f,(float)(Rand.NextDouble()*2)-1f), 3000, true, new Rectangle(0, 0, 8, 8), 0f, new Color(tint.ToVector3() * 0.9f));
                tempV = Vector2.Lerp(pos, pos+velocity, amount);
                amount += 0.1f;
            }
        }

        internal void Reset()
        {
            foreach (Particle p in Particles)
            {
                p.Active = false;
            }
        }

        bool CheckCollision(Vector2 pos, float landingHeight, Map gameMap, List<int> levelSectors, Dictionary<int, MapObjectLayer> walkableLayers, int ghs)
        {
            for (int i = 0; i < levelSectors.Count; i++)
            {
                if (i < ghs - 1 || i > ghs + 1) continue;
                MapObjectLayer walkableLayer = walkableLayers[levelSectors[i]];

                for (int o = 0; o < walkableLayer.Objects.Count; o++)
                {
                    if (Helper.IsPointInShape(new Vector2(pos.X - ((gameMap.Width * gameMap.TileWidth) * i), landingHeight), walkableLayer.Objects[o].LinePoints)) return false;
                }
            }

            return true;
        }
    }
}
