using Microsoft.Xna.Framework;
using Multiplayer_Games_Programming_Framework;
using System;
using System.Collections.Generic;
using nkast.Aether.Physics2D.Dynamics;
using Multiplayer_Games_Programming_Framework.Core;
using System.Data;
using System.Diagnostics;
using Multiplayer_Games_Programming_Framework.GameCode.Components;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Framework
{
	internal class GameScene : Scene
	{
		List<GameObject> m_GameObjects = new();

		BallGO m_Ball;
		PaddleGO m_PlayerPaddle;
		PaddleGO m_RemotePaddle;
		TrophyGO m_ScoreTrophy;
		ScoreTrophy m_ScoreController;

		BallControllerComponent m_BallController;
		
		GameModeState m_GameModeState;

		float m_GameTimer;
		bool m_GameEnd = false;
		bool m_LoadMenu = false;
		// negative is left score
		int m_Score = 0;
		int m_MaxScore = 4;

		public GameScene(SceneManager manager) : base(manager)
		{
			m_GameModeState = GameModeState.AWAKE;
		}

		public void ChangeScore(bool add)
		{         
			if (add)
			{
				m_Score++;
			}
			else
			{
                m_Score--;
			}
			
			NetworkManager.Instance.SendPacket(new ScorePacket(m_Score));
			UpdateScoreUI();
		}
		public void UpdateScore(int score)
		{
			m_Score = score;
            UpdateScoreUI();
        }

		public void UpdateScoreUI()
		{
			m_ScoreController.UpdatePos(m_Score);
			if(m_Score >= m_MaxScore || m_Score <= -m_MaxScore)
			{
				m_GameEnd = true;
			}
        }

		public void LoadMenu()
		{
			m_LoadMenu = true;
        }

		public override void LoadContent()
		{
			base.LoadContent();

			NetworkManager.Instance.ScoreAction = UpdateScore;
			NetworkManager.Instance.PlayerLeft = LoadMenu;

			float screenWidth = Constants.m_ScreenWidth;
			float screenHeight = Constants.m_ScreenHeight;

			m_Ball = GameObject.Instantiate<BallGO>(this, new Transform(new Vector2(screenWidth / 2, screenHeight / 2), new Vector2(Constants.m_ScalarWidth, Constants.m_ScalarHeight), 0));
			m_BallController = m_Ball.GetComponent<BallControllerComponent>();

			if (NetworkManager.Instance.PlayerOne)
			{
				m_PlayerPaddle = GameObject.Instantiate<PaddleGO>(this, new Transform(new Vector2(100 * Constants.m_ScalarWidth, 500 * Constants.m_ScalarHeight), new Vector2(5 * Constants.m_ScalarWidth, 20 * Constants.m_ScalarHeight), 0));
                m_PlayerPaddle.AddComponent(new PaddleController(m_PlayerPaddle));

				m_RemotePaddle = GameObject.Instantiate<PaddleGO>(this, new Transform(new Vector2(screenWidth - (100 * Constants.m_ScalarWidth), 500 * Constants.m_ScalarHeight), new Vector2(5 * Constants.m_ScalarWidth, 20 * Constants.m_ScalarHeight), 0));
                m_RemotePaddle.AddComponent(new PaddleNetworkController(m_RemotePaddle, 1));
			}
			else
			{
				m_RemotePaddle = GameObject.Instantiate<PaddleGO>(this, new Transform(new Vector2(100*Constants.m_ScalarWidth, 500*Constants.m_ScalarHeight), new Vector2(5 * Constants.m_ScalarWidth, 20 * Constants.m_ScalarHeight), 0));
				m_RemotePaddle.AddComponent(new PaddleNetworkController(m_RemotePaddle, 0));

				m_PlayerPaddle = GameObject.Instantiate<PaddleGO>(this, new Transform(new Vector2(screenWidth - (100 * Constants.m_ScalarWidth), 500 * Constants.m_ScalarHeight), new Vector2(5 * Constants.m_ScalarWidth, 20 * Constants.m_ScalarHeight), 0));
				m_PlayerPaddle.AddComponent(new PaddleController(m_PlayerPaddle));
			}

			m_ScoreTrophy = GameObject.Instantiate<TrophyGO>(this, new Transform(new Vector2(screenWidth / 2.0f, 100 * Constants.m_ScalarHeight), new Vector2(Constants.m_ScalarWidth / 8.0f, Constants.m_ScalarHeight / 8.0f), 0));
			m_ScoreController = new ScoreTrophy(m_ScoreTrophy, m_MaxScore);
			m_ScoreTrophy.AddComponent(m_ScoreController);

			//Border
			Vector2[] wallPos = new Vector2[]
			{
				new Vector2(screenWidth/2, 0), //top
				new Vector2(screenWidth, screenHeight/2), //right
				new Vector2(screenWidth/2, screenHeight), //bottom
				new Vector2(0, screenHeight/2) //left
			};

			Vector2[] wallScales = new Vector2[]
			{
				new Vector2(screenWidth/10, Constants.m_ScalarHeight*10), //top
				new Vector2(Constants.m_ScalarWidth*10, screenHeight/10), //right
				new Vector2(screenWidth/10, Constants.m_ScalarHeight*10), //bottom
				new Vector2(Constants.m_ScalarWidth*10, screenHeight/10) //left
			};

			// walls
			for (int i = 0; i < 4; i++)
			{
				GameObject go = GameObject.Instantiate<GameObject>(this, new Transform(wallPos[i], wallScales[i], 0));
				SpriteRenderer sr = go.AddComponent(new SpriteRenderer(go, "Square(10x10)"));
				Rigidbody rb = go.AddComponent(new Rigidbody(go, BodyType.Static, 10, sr.m_Size / 2));
				rb.CreateRectangle(sr.m_Size.X, sr.m_Size.Y, 0.0f, 1.0f, Vector2.Zero, Constants.GetCategoryByName("Wall"), Constants.GetCategoryByName("All"));

				if(i % 2 != 0 && NetworkManager.Instance.PlayerOne)
					go.AddComponent(new ChangeScoreOnCollision(go, i == 3, m_BallController.Reset, ChangeScore));
				m_GameObjects.Add(go);
			}
		}

		protected override string SceneName()
		{
			return "GameScene";
		}

		protected override World CreateWorld()
		{
			return new World(Constants.m_Gravity);
		}

		public override void Update(float deltaTime)
		{
			base.Update(deltaTime);

			m_GameTimer += deltaTime;

			if(m_LoadMenu)
			{
                m_Manager.LoadScene(new MenuScene(m_Manager));
            }

			switch (m_GameModeState)
			{
				case GameModeState.AWAKE:
					m_GameModeState = GameModeState.STARTING;
					break;

				case GameModeState.STARTING:
					m_BallController.Init(20, new Vector2(1.0f, 0.0f));
					m_BallController.StartBall();
					
					m_GameModeState = GameModeState.PLAYING;

					break;

				case GameModeState.PLAYING:
					if(m_GameEnd)
					{
						m_Ball.Destroy();
						m_GameModeState = GameModeState.ENDING;
                        Debug.WriteLine("Game Over");
                    }

					break;

				case GameModeState.ENDING:

					//Debug.WriteLine("Game Over");
					break;
				default:
					break;
			}
		}
	}
}