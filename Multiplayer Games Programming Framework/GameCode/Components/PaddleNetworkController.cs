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
		bool m_PositionDirty = false;
		Vector2 m_Position;

		public PaddleNetworkController(GameObject gameObject, int index) : base(gameObject)
		{
			m_Index = index;
            m_Speed = 10;
            NetworkManager.m_Instance.m_PositionActions.TryAdd(0, PositionEvent);
		}

		protected override void Start(float deltaTime)
		{
			m_Rigidbody = m_GameObject.GetComponent<Rigidbody>();
        }

        protected override void Update(float deltaTime)
        {
			if(m_PositionDirty)
			{
                m_Rigidbody.UpdatePosition(m_Position);
            }
		}

		public void PositionEvent(Vector2 position)
		{
            m_Position = position;
			m_PositionDirty = true;
        }
	}
}
