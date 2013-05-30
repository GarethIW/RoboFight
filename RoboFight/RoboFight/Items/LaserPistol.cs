using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboFight
{
    class LaserPistol : Item
    {
        public LaserPistol(Texture2D tex, Rectangle src) : base(ItemType.Projectile, tex, src) {
            Name = "laserpistol";
            Health = 100f;
            Range = 500f;
            Cooldown = 500;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Use(int faceDir, float attackCharge, Robot gameHero)
        {
            if (coolDownTime > 0) return;

            if (Owner.IsPlayer)
            {
                ProjectileManager.Instance.Add(Owner.Position + new Vector2(Owner.faceDir * 60, -107), Owner.landingHeight - 1f, new Vector2(Owner.faceDir * 10f, 0f), 2000, true, ProjectileType.Laser, 35f, Color.Red);

                Health -= 5f;
            }
            else
            {
                if (Owner.landingHeight > gameHero.landingHeight-30 && Owner.landingHeight<gameHero.landingHeight+30)
                {
                    ProjectileManager.Instance.Add(Owner.Position + new Vector2(Owner.faceDir * 60, -107), Owner.landingHeight - 1f, new Vector2(Owner.faceDir * 10f, 0f), 2000, false, ProjectileType.Laser, 35f, Color.Red);

                    Health -= 1f;
                }
            }
            base.Use(faceDir, attackCharge, gameHero);
        }


    }
}
