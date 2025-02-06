using Microsoft.Xna.Framework;
using Multiplayer_Games_Programming_Framework.Core;
using System;

namespace Multiplayer_Games_Programming_Framework.GameCode.Components
{
	internal class PaddleNetworkController : Component
	{
		int m_Index;
		float m_Speed;
		Rigidbody m_Rigidbody;
		bool m_PositionDirty = false;
		Vector2 m_Velocity;
		Vector2 m_VelocityCorrection;

		public PaddleNetworkController(GameObject gameObject, int index) : base(gameObject)
		{
			m_Index = index;
            m_Speed = 10 * Constants.m_ScalarHeight;
            NetworkManager.Instance.PaddleActions[1] = PaddleEvent;
		}

		protected override void Start(float deltaTime)
		{
			m_Rigidbody = m_GameObject.GetComponent<Rigidbody>();
            m_Velocity = Vector2.Zero;
        }

        protected override void Update(float deltaTime)
        {
			if (m_PositionDirty)
			{
				m_Rigidbody.m_Body.LinearVelocity = m_Velocity + m_VelocityCorrection;
				m_PositionDirty = false;
			}
			else
			{
				m_Rigidbody.m_Body.LinearVelocity = m_Velocity;
            }
		}

		public void PaddleEvent(Vector2 position, float input)
        {
			Vector2 pos = m_Transform.Position;
			Vector2 direction = position - pos;
			m_VelocityCorrection = direction * 0.016f;

			m_Velocity = Vector2.Zero;
			m_Velocity.Y = input * m_Speed;

            m_PositionDirty = true;
        }
	}
}
