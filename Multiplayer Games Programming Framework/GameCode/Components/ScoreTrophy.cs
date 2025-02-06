using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using Multiplayer_Games_Programming_Framework.Core;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Framework
{
    internal class ScoreTrophy : Component
    {
        Vector2 m_MidPosition;
        Vector2 m_TranslationPerScore;
        int m_MaxScore;
        public ScoreTrophy(GameObject gameObject, int maxScore) : base(gameObject)
        {
            m_MaxScore = maxScore;
        }

        protected override void Start(float deltaTime)
        {
            m_MidPosition = m_Transform.Position;
            m_TranslationPerScore = Vector2.Zero;
            m_TranslationPerScore.X = m_MidPosition.X / m_MaxScore;
        }

        public void UpdatePos(int score)
        {
            m_Transform.Position = m_MidPosition + (score * m_TranslationPerScore);
        }
    }
}
