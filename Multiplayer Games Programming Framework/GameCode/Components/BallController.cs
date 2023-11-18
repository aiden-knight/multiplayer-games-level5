using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using Multiplayer_Games_Programming_Framework.Core;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Framework
{
	internal class BallControllerComponent : Component
	{
		float m_Speed;
		Vector2 m_InitDirection;
		Rigidbody m_Rigidbody;

		bool m_Changed = false;
		Vector2 m_Position;
		Vector2 m_Velocity;
		public BallControllerComponent(GameObject gameObject) : base(gameObject)
		{
		}

		protected override void Start(float deltaTime)
		{
			m_Rigidbody = m_GameObject.GetComponent<Rigidbody>();
			m_Position = m_Transform.Position;
			NetworkManager.Instance.m_BallAction = PositionVelocityEvent;
		}
		public void Init(float speed, Vector2 direction)
		{
			float modifier = (Constants.m_ScalarHeight + Constants.m_ScalarWidth) / 2.0f;
			m_Speed = speed * modifier;
			m_InitDirection = direction;
		}

		public void StartBall()
		{
			m_Rigidbody.m_Body.LinearVelocity = (m_InitDirection * m_Speed);
        }

        protected override void LateUpdate(float deltaTime)
        {
            base.LateUpdate(deltaTime);

            if (m_Changed)
            {
                m_Rigidbody.UpdatePosition(m_Position);
                m_Rigidbody.m_Body.LinearVelocity = m_Velocity;
                m_Changed = false;
            }
        }

        protected override void OnCollisionEnter(Fixture sender, Fixture other, Contact contact)
        {
            if (NetworkManager.Instance.PlayerID == 0)
            {
                Vector2 normal = contact.Manifold.LocalNormal;
				Vector2 velocity = m_Rigidbody.m_Body.LinearVelocity;
				Vector2 reflection =  Vector2.Reflect(velocity, normal);
				m_Rigidbody.m_Body.LinearVelocity = reflection * 1.0f;

				Vector2 pos = m_Transform.Position;
				Vector2 newVelocity = m_Rigidbody.m_Body.LinearVelocity;

                BallPacket ballPacket = new BallPacket(pos.X, pos.Y,newVelocity.X, newVelocity.Y);
				NetworkManager.Instance.SendPacket(ballPacket);
			}
		}

		public void PositionVelocityEvent(Vector2 pos, Vector2 velocity)
		{
			m_Position = pos;
			m_Velocity = velocity;
			m_Changed = true;
		}
	}
}
