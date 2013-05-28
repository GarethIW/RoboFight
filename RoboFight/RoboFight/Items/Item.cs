using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledLib;

namespace RoboFight
{
    public enum ItemType
    {
        Melee,
        Projectile
    }

    public class Item
    {
        public static Random rand = new Random();

        public ItemType Type;
        public Vector2 Position;
        public Vector2 DroppedPosition;
        public Vector2 Speed;
        public string Name;
        public bool InWorld = false;
        public bool Dead = false;
        public float Health;
        public Robot Owner;

        Texture2D itemTexture;
        Rectangle itemSource;

        float alpha = 1f;

        public Item(ItemType type, Texture2D tex, Rectangle src)
        {
            itemTexture = tex;
            itemSource = src;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (InWorld)
            {
                Speed.Y += 0.25f;

                

                if (Position.Y >= DroppedPosition.Y)
                {
                    if (Speed.Y > 2f)
                    {
                        Speed.Y = -(Speed.Y * 0.6f);
                    }
                    else
                        Speed.Y = 0f;
                }

                Position.Y += Speed.Y;
            }

            if (Health <= 0f)
            {
                if(!InWorld)
                {
                    InWorld = true;
                    Owner.Item = null;
                    DroppedPosition = Owner.Position;
                    Position = Owner.Position + new Vector2(Owner.faceDir * 75, -75);
                }
                alpha -= 0.01f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
                if(alpha<=0f)
                    Dead = true;
            }
        }

        public virtual void Draw(SpriteBatch sb, Camera gameCamera)
        {
            sb.Draw(itemTexture, Position, itemSource, Color.White * alpha, 0f, new Vector2(itemSource.Width, itemSource.Height)/2,1f, SpriteEffects.None, 1);
        }

        public virtual void Use(int faceDir, float attackCharge, Robot gameHero)
        {

        }
    }
}
