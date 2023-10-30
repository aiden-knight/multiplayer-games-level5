﻿using Microsoft.Xna.Framework;
using Multiplayer_Games_Programming_Framework.Core;
using System;

namespace Multiplayer_Games_Programming_Framework.GameCode.Components
{
	internal class PaddleNetworkController : Component
	{
		int m_Index;
		float m_Speed;
		Rigidbody m_Rigidbody;
		Vector2 m_Input;

		public PaddleNetworkController(GameObject gameObject, int index) : base(gameObject)
		{
			m_Index = index;
            m_Speed = 10;
            NetworkManager.m_Instance.PositionEvent += new EventHandler<PositionEventArgs>(PositionEvent);
		}

		protected override void Start(float deltaTime)
		{
			m_Rigidbody = m_GameObject.GetComponent<Rigidbody>();
            m_Input = Vector2.Zero;
        }

        protected override void Update(float deltaTime)
        {
            m_Rigidbody.m_Body.LinearVelocity = (m_Transform.Up * m_Input.Y * m_Speed);
        }

		public void PositionEvent(object sender, PositionEventArgs e)
		{
			m_Input = e.position;
		}
	}
}
