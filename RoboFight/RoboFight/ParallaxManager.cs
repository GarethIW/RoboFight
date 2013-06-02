using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboFight
{
    public class ParallaxLayer
    {
        public Texture2D Texture;
        public Vector2 Position;
        public float ScrollSpeed;
        public bool PositionFromBottom;
        public Color Tint;

        public ParallaxLayer(Texture2D tex, Vector2 pos, float speed, bool bottom, Color tint)
        {
            Texture = tex;
            Position = pos;
            ScrollSpeed = speed;
            PositionFromBottom = bottom;
            Tint = tint;
        }
    }

    public class ParallaxManager
    {
        public List<ParallaxLayer> Layers = new List<ParallaxLayer>();

        Viewport viewport;

        Vector2 scrollPosition;

        public ParallaxManager(Viewport vp)
        {
            viewport = vp;
        }

        public void Update(GameTime gameTime, Vector2 scrollPos)
        {
            scrollPosition = scrollPos;// -new Vector2(GameManager.Camera.Width, GameManager.Camera.Height) / 2;

            foreach (ParallaxLayer l in Layers)
            {
                l.Position.Y = 1500 + (scrollPosition.Y * l.ScrollSpeed);
                l.Position.X = scrollPosition.X * l.ScrollSpeed;
            }
        }


        public void Draw(SpriteBatch spriteBatch)
        {

            foreach (ParallaxLayer l in Layers)
            {

                for (float x = l.Position.X-600; x < scrollPosition.X + spriteBatch.GraphicsDevice.Viewport.Width; x += l.Texture.Width)
                {
                    if (l.Position.X + x > -l.Texture.Width)
                    {
                        spriteBatch.Draw(l.Texture, (l.PositionFromBottom?new Vector2(l.Position.X,spriteBatch.GraphicsDevice.Viewport.Height-l.Position.Y):l.Position) + new Vector2(x, 0), null, l.Tint, 0f, new Vector2(l.Texture.Width, l.Texture.Height)/2, 1f, SpriteEffects.None, 1);
                    }
                }
                  
            }
        }
    }
}
