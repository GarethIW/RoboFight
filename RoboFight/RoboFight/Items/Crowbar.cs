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
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Use()
        {
            base.Use();
        }
    }
}
