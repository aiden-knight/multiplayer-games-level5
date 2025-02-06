using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer_Games_Programming_Framework
{
    internal class TrophyGO : GameObject
    {
        public TrophyGO(Scene scene, Transform transform) : base(scene, transform)
        {
            //https://opengameart.org/content/golden-trophy-game-ornament
            SpriteRenderer sr = AddComponent(new SpriteRenderer(this, "golden_trophy"));
            sr.m_DepthLayer = 0;
        }
    }
}
