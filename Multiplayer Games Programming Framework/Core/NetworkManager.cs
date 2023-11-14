using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Multiplayer_Games_Programming_Packet_Library;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using System.Net;
using System.Xml;
using System.Threading.Tasks;

namespace Multiplayer_Games_Programming_Framework.Core
{
	internal class NetworkManager
	{
		private static NetworkManager Instance;

		public static NetworkManager m_Instance
		{
			get
			{
				if (Instance == null)
				{
					return Instance = new NetworkManager();
				}
			
				return Instance;
			}
		}
		
		TcpClient m_TcpClient;
		UdpClient m_UdpClient;
		bool m_UdpHandshakeCompleted = false;

		NetworkStream m_Stream;
		StreamReader m_StreamReader;
		StreamWriter m_StreamWriter;
		int m_clientID;
		public int m_playerID { get; private set; }
		public bool m_Playable { get; private set; }

		public Dictionary<int, Action<Vector2>> m_PositionActions;
		public Action<Vector2, Vector2> m_BallAction;
		public Action m_PlayAction;

		NetworkManager()
		{
			m_TcpClient = new TcpClient();
			m_UdpClient = new UdpClient();

            m_clientID = -1;
			m_Playable = false;
			m_PositionActions = new Dictionary<int, Action<Vector2>>();
		}

		public bool Connect(string ip, int port)
		{
			try
			{
                IPAddress iPAddress = IPAddress.Parse(ip);
                m_TcpClient.Connect(iPAddress, port);
				m_UdpClient.Connect(iPAddress, port);

				m_Stream = m_TcpClient.GetStream();
                m_StreamReader = new StreamReader(m_Stream, Encoding.UTF8);
                m_StreamWriter = new StreamWriter(m_Stream, Encoding.UTF8);

				Run();
				return true;
            }
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}

			return false;
		}

		public void Run()
		{
			UdpProcessServerResponse();

			Thread tcpThread = new Thread(new ThreadStart(TcpProcessServerResponse));
			tcpThread.Name = "TCP THREAD";
            tcpThread.Start();

        }

		private void ConnectionClosed()
		{


			m_TcpClient.Close();
            m_UdpClient.Close();
        }

		private void TcpProcessServerResponse()
		{
			while(m_TcpClient.Connected)
			{
				string packetJSON;

                try
				{
                    packetJSON = m_StreamReader.ReadLine();
					if (packetJSON == null) continue;
				}
				catch
				{
					ConnectionClosed();
					continue;
				}
					
                Packet? p = Packet.Deserialize(packetJSON);
				if (p == null) continue;
                    
				PacketType type = p.m_Type;
				switch (type)
				{
					case PacketType.MESSAGE:
                        string message = ((MessagePacket)p).message;
                        Debug.WriteLine(message);
					break;
					case PacketType.LOGIN:
						LoginPacket loginPacket = (LoginPacket)p;
						m_clientID = loginPacket.ID;
						SendPacketUdp(loginPacket);
					break;
					case PacketType.GAME_READY:
						GameReadyPacket gameReadyPacket = (GameReadyPacket)p;
						m_playerID = gameReadyPacket.playerID;
						m_Playable = true;
					break;
					case PacketType.POSITION:
						PositionPacket posPacket = (PositionPacket)p;
						Vector2 pos = new Vector2(posPacket.x, posPacket.y);
						if(m_PositionActions.ContainsKey(0))
							m_PositionActions[0]?.Invoke(pos);
					break;
					case PacketType.BALL:
						BallPacket ballPacket = (BallPacket)p;
						Vector2 ballPos = new Vector2(ballPacket.x, ballPacket.y);
						Vector2 ballVelocity = new Vector2(ballPacket.vX, ballPacket.vY);
						m_BallAction?.Invoke(ballPos, ballVelocity);
					break;
					case PacketType.PLAY:
						m_PlayAction?.Invoke();
					break;
                }
			}
		}

        async Task UdpProcessServerResponse()
        {
            m_UdpHandshakeCompleted = false;
            try
            {
                while (m_TcpClient.Connected)
                {
                    UdpReceiveResult receiveResult = await m_UdpClient.ReceiveAsync();
                    byte[] receivedData = receiveResult.Buffer;

                    string packetJSON = Encoding.UTF8.GetString(receivedData, 0, receivedData.Length);

                    Packet? p = Packet.Deserialize(packetJSON);
                    if (p == null) continue;

                    PacketType type = p.m_Type;
                    switch (type)
                    {
                        case PacketType.LOGIN:
							m_UdpHandshakeCompleted = true;
							Debug.WriteLine("UDP Handshake Completed");
                        break;
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.WriteLine("Client UDP Read Method exception: " + e.Message);
            }
        }

		public void SendPacket(Packet packet)
		{
            string data = packet.ToJson();
            m_StreamWriter.WriteLine(data);
            m_StreamWriter.Flush();
        }

		public void SendPacketUdp(Packet packet)
		{
			if(packet.m_Type != PacketType.LOGIN && !m_UdpHandshakeCompleted)
			{
				Debug.WriteLine("Tried to send udp packet before handshake completed");
				return;
			}

			string data = packet.ToJson();
			byte[] bytes = Encoding.UTF8.GetBytes(data);

            m_UdpClient.Send(bytes, bytes.Length);
        }
		// Close network manager listeners on closing of game
		public void Close()
		{
			m_TcpClient.Close();
			m_UdpClient.Close();
		}
		public void Login()
		{
			SendPacket(new LoginPacket());
		}

		public void SendPosition(Vector2 pos)
		{
            SendPacket(new PositionPacket(pos.X, pos.Y));
        }
	}
}
