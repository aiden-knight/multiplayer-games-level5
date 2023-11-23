using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using Multiplayer_Games_Programming_Framework.Core;
using Multiplayer_Games_Programming_Packet_Library;
using System.Diagnostics;

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

		Vector2 m_StartPos;
		public BallControllerComponent(GameObject gameObject) : base(gameObject)
		{
		}

		protected override void Start(float deltaTime)
		{
			m_Rigidbody = m_GameObject.GetComponent<Rigidbody>();
			m_StartPos = m_Transform.Position;
			m_Position = m_Transform.Position;
			NetworkManager.Instance.BallAction = PositionVelocityEvent;
		}
		public void Init(float speed, Vector2 direction)
		{
			float modifier = (Constants.m_ScalarHeight + Constants.m_ScalarWidth) / 2.0f;
			m_Speed = speed * modifier;
			m_InitDirection = direction;
		}

		public void StartBall()
		{
			m_Rigidbody.m_Body.LinearVelocity = (Vector2.Normalize(m_InitDirection) * m_Speed);
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
			if (!NetworkManager.Instance.PlayerOne) return;
			GameObject obj = other.Body.Tag as GameObject;
			if (obj?.GetComponent<ChangeScoreOnCollision>() != null) return;

            // change balls direction based off where it collides
            if (other.Body.Tag is PaddleGO otherObj && contact.Manifold.LocalPoint.Y == 0)
            {
                float yPos = otherObj.m_Transform.Position.Y;
                float ySize = otherObj.sizeY;
                float top = yPos + ySize / 2;
                float bot = yPos - ySize / 2;

                float ballY = m_Transform.Position.Y;
                float amount = (ballY - bot) / (top - bot) * (0.8f / 0.5f);
                amount -= 0.8f;

                Vector2 normal = contact.Manifold.LocalNormal;
                Vector2 velocity = m_Rigidbody.m_Body.LinearVelocity;
                float direction = velocity.X > 0 ? -1 : 1;
                velocity.X = (1 - amount) * direction;
                velocity.Y = amount;

                velocity = m_Speed * Vector2.Normalize(velocity);
                m_Rigidbody.m_Body.LinearVelocity = velocity * 1.0f;
            }
            else
            {
                Vector2 normal = contact.Manifold.LocalNormal;
                Vector2 velocity = m_Rigidbody.m_Body.LinearVelocity;
                Vector2 reflection = Vector2.Reflect(velocity, normal) * 1.0f;
                m_Rigidbody.m_Body.LinearVelocity = m_Speed * Vector2.Normalize(reflection);
            }

            Vector2 pos = m_Transform.Position;
			Vector2 newVelocity = m_Rigidbody.m_Body.LinearVelocity;

            BallPacket ballPacket = new BallPacket(pos.X, pos.Y,newVelocity.X, newVelocity.Y);
			NetworkManager.Instance.SendPacket(ballPacket);
		}

		public void PositionVelocityEvent(Vector2 pos, Vector2 velocity)
		{
			m_Position = pos;
			m_Velocity = velocity;
			m_Changed = true;
		}

		public void Reset(bool rightScored)
		{
            m_Velocity = (Vector2.Normalize(m_InitDirection) * m_Speed);
            if (!rightScored)
			{
                m_Velocity *= -1;
            }
			m_Position = m_StartPos;           

            BallPacket ballPacket = new BallPacket(m_Position.X, m_Position.Y, m_Velocity.X, m_Velocity.Y);
            NetworkManager.Instance.SendPacket(ballPacket);
            m_Changed = true;
        }
	}
}
