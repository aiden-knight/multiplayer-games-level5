using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using System;

namespace Multiplayer_Games_Programming_Framework
{
	internal class ChangeScoreOnCollision : Component
	{
		public bool LeftWall { get; private set; }
		Action<bool> m_ResetBall;
		Action<bool> m_UpdateScore;
		public ChangeScoreOnCollision(GameObject gameObject, bool leftWall, Action<bool> resetBall, Action<bool> updateScore) : base(gameObject)
		{
            LeftWall = leftWall;
			m_ResetBall = resetBall;
			m_UpdateScore = updateScore;
		}

		protected override void OnCollisionEnter(Fixture sender, Fixture other, Contact contact)
		{
			m_ResetBall.Invoke(LeftWall);
			m_UpdateScore.Invoke(LeftWall);
		}
	}
}
