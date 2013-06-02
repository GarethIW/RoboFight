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

    public class HUD
    {
        Viewport viewport;

        Texture2D texHUD;
        SpriteFont fontHUD;

        public float Alpha = 1f;

        int healthWidth = 120;
        int weapWidth = 120;

        static Random randomNumber = new Random();

        double lowHealthBlinkTime = 0;
        bool lowHealthBlink = false;

        int Score;

        public HUD(Viewport vp)
        {
            viewport = vp;
        }

        public void LoadContent(ContentManager content)
        {
            texHUD = content.Load<Texture2D>("particles");
            fontHUD = content.Load<SpriteFont>("font");
        }

        public void Update(GameTime gameTime, Robot gameHero)
        {
            healthWidth = (int)gameHero.Health;
            if (gameHero.Item != null)
                weapWidth = (int)gameHero.Item.Health;
            else weapWidth = 0;

            Score = gameHero.Score;

            if (gameHero.Health <= 25f)
            {
                lowHealthBlinkTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (lowHealthBlinkTime > 500)
                {
                    lowHealthBlinkTime = 0;
                    lowHealthBlink = !lowHealthBlink;
                    //if (lowHealthBlink) AudioController.PlaySFX("alert", 0.8f, 0f, 0f);
                }
            }
            else
                lowHealthBlink = false;

        }


        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 pos = new Vector2(40, 20);

            for (int i = 0; i < healthWidth; i++)
            {
                spriteBatch.Draw(texHUD, pos, new Rectangle(11, 0, 15, 8), Color.Red, 0f, new Vector2(7,4), 1f, i%2==0?SpriteEffects.None:SpriteEffects.FlipVertically, 1);
                pos.X += 10;
            }

            for (int i = healthWidth; i < 121; i++)
            {
                spriteBatch.Draw(texHUD, pos, new Rectangle(11, 0, 15, 8), Color.Red*0.1f, 0f, new Vector2(7, 4), 1f, i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 1);
                pos.X += 10;
            }

            pos = new Vector2(40, 30);

            for (int i = 0; i < weapWidth; i++)
            {
                spriteBatch.Draw(texHUD, pos, new Rectangle(11, 0, 15, 8), Color.DeepSkyBlue, 0f, new Vector2(7, 4), 1f, i % 2 != 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 1);
                pos.X += 10;
            }
            for (int i = weapWidth; i < 121; i++)
            {
                spriteBatch.Draw(texHUD, pos, new Rectangle(11, 0, 15, 8), Color.DeepSkyBlue*0.1f, 0f, new Vector2(7, 4), 1f, i % 2 != 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 1);
                pos.X += 10;
            }

            Helper.ShadowText(spriteBatch, fontHUD, Score.ToString(), new Vector2(20, viewport.Height-50), Color.White, Vector2.Zero, 1f);

        }


        void ShadowText(SpriteBatch sb, string text, Vector2 pos, Color col, Vector2 off, float scale)
        {
            sb.DrawString(fontHUD, text, pos + (Vector2.One * 2f), new Color(0,0,0,col.A), 0f, off, scale, SpriteEffects.None, 1);
            sb.DrawString(fontHUD, text, pos, col, 0f, off, scale, SpriteEffects.None, 1);
        }
    }
}
