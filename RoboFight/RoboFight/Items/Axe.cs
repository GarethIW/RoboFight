using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboFight
{
    class Axe : Item
    {
        public Axe(Texture2D tex, Rectangle src) : base(ItemType.Melee, tex, src) {
            Name = "axe";
            Range = 200f;
            Cooldown = 700;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Use(int faceDir, float attackCharge, Robot gameHero)
        {
            if (Owner.IsPlayer)
            {
                if (EnemyManager.Instance.CheckAttack(Owner.Position, faceDir, 50f, Range, 4, gameHero)) Health -= 15f;
            }
            else
            {
                if ((Owner.Position - gameHero.Position).Length() < 100f)
                {
                    gameHero.DoHit(Position, 25f, faceDir, gameHero);
                    Health -= 2f;
                }
            }
            base.Use(faceDir, attackCharge, gameHero);
        }
    }
}
