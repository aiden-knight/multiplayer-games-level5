﻿using Myra;
using Myra.Graphics2D.UI;
using nkast.Aether.Physics2D.Dynamics;
using Multiplayer_Games_Programming_Framework.Core;
using System.Diagnostics;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Framework
{
	internal class MenuScene : Scene
	{
		private Desktop m_Desktop;
		public MenuScene(SceneManager manager) : base(manager)
		{
			manager.m_Game.IsMouseVisible = true;
		}

		protected override World CreateWorld()
		{
			return null;
		}

		protected override string SceneName()
		{
			return "Main Menu";
		}

		public override void LoadContent()
		{
			MyraEnvironment.Game = m_Manager.m_Game;

			var grid = new Grid
			{
				ShowGridLines = false,
				RowSpacing = 8,
				ColumnSpacing = 8
			};

			int cols = 4;
			for (int i = 0; i < cols; ++i)
			{
				grid.ColumnsProportions.Add(new Proportion(ProportionType.Part));
			}

			int rows = 5;
			for (int i = 0; i < rows; ++i)
			{
				grid.RowsProportions.Add(new Proportion(ProportionType.Part));
			}

			m_Desktop = new Desktop();
			m_Desktop.Root = grid;


			var LoginButton = new TextButton();
			LoginButton.Text = "Login";
			LoginButton.GridRow = 2;
			LoginButton.GridColumn = 1;
			LoginButton.GridColumnSpan = 2;
			LoginButton.HorizontalAlignment = HorizontalAlignment.Center;
			LoginButton.VerticalAlignment = VerticalAlignment.Center;
			LoginButton.Width = (Constants.m_ScreenWidth / cols) * LoginButton.GridColumnSpan;
			LoginButton.Height = (Constants.m_ScreenHeight / rows) * LoginButton.GridRowSpan;
			grid.Widgets.Add(LoginButton);

			var PlayButton = new TextButton();
			PlayButton.Text = "Play";
			PlayButton.GridRow = 3;
			PlayButton.GridColumn = 1;
			PlayButton.GridColumnSpan = 2;
			PlayButton.HorizontalAlignment = HorizontalAlignment.Center;
			PlayButton.VerticalAlignment = VerticalAlignment.Center;
			PlayButton.Width = (Constants.m_ScreenWidth / cols) * LoginButton.GridColumnSpan;
			PlayButton.Height = (Constants.m_ScreenHeight / rows) * LoginButton.GridRowSpan;
			PlayButton.Enabled = false;
			PlayButton.Id = "PlayButton";
			grid.Widgets.Add(PlayButton);

			PlayButton.Click += (s, a) =>
			{
				NetworkManager.m_Instance.SendPacket(new PlayPacket());
			};
			NetworkManager.m_Instance.m_PlayAction = Play;

			var childPanel = new Panel();
			childPanel.GridColumn = 0;
			childPanel.GridRow = 0;

			grid.Widgets.Add(childPanel);

			LoginButton.Click += (s, a) =>
			{
				if (NetworkManager.m_Instance.Connect("127.0.0.1", 4444))
				{
					NetworkManager.m_Instance.Login();
					LoginButton.Enabled = false;
				}
				else
				{
					Debug.WriteLine("Failed to connect");
				}
			};
		}

		public override void Draw(float deltaTime)
		{
			base.Draw(deltaTime);
			m_Desktop.Render();
		}

		public override void Update(float deltaTime)
		{
			base.Update(deltaTime);

			if(NetworkManager.m_Instance.m_Playable && NetworkManager.m_Instance.m_playerID == 0)
			{
				m_Desktop.GetWidgetByID("PlayButton").Enabled = true;
			}

			if(m_DoPlay)
			{
                m_Manager.LoadScene(new GameScene(m_Manager));
				m_DoPlay = false;
            }
		}

		bool m_DoPlay = false;
		public void Play()
		{
			m_DoPlay = true;
		}
	}
}
