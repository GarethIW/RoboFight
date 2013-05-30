using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoboFight
{
    class Crowbar : Item
    {
        public Crowbar(Texture2D tex, Rectangle src) : base(ItemType.Melee, tex, src) {
            Name = "crowbar";
            Health = 100f;
            Range = 100f;
            Cooldown = 300;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Use(int faceDir, float attackCharge, Robot gameHero)
        {
            if (Owner.IsPlayer)
            {
                if (EnemyManager.Instance.CheckAttack(Owner.Position, faceDir, 35f, 100f, 2)) Health -= 10f;
            }
            else
            {
                if ((Owner.Position - gameHero.Position).Length() < 100f)
                {
                    gameHero.DoHit(Position, 35f, faceDir);
                    Health -= 2f;
                }
            }
            base.Use(faceDir, attackCharge, gameHero);
        }
    }
}
