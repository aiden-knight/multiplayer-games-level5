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

namespace Multiplayer_Games_Programming_Framework.Core
{
	public class PositionEventArgs : EventArgs
	{
		public PositionEventArgs(Vector2 position)
		{
			this.position = position;
		}
		public Vector2 position;
	}

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
		NetworkStream m_Stream;
		StreamReader m_StreamReader;
		StreamWriter m_StreamWriter;
		public int m_ID { get; private set; }
		public bool m_Playable { get; private set; }

		public event EventHandler<PositionEventArgs> PositionEvent;

		NetworkManager()
		{
			m_TcpClient = new TcpClient();
            m_ID = -1;
			m_Playable = false;
		}

		public bool Connect(string ip, int port)
		{
			try
			{
                IPAddress iPAddress = IPAddress.Parse(ip);
                m_TcpClient.Connect(iPAddress, port);
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
			Thread tcpThread = new Thread(new ThreadStart(TcpProcessServerResponse));
			tcpThread.Name = "TCP THREAD";
            tcpThread.Start();
        }

		private void TcpProcessServerResponse()
		{
			try
			{
				while(m_TcpClient.Connected)
				{
                    string? packetJSON = m_StreamReader.ReadLine();
					if (packetJSON == null) continue;
					
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
							m_ID = loginPacket.ID;
						break;
						case PacketType.GAME_READY:
							m_Playable = true;
						break;
						case PacketType.POSITION:
							PositionPacket posPacket = (PositionPacket)p;
							Vector2 pos = new Vector2(posPacket.x, posPacket.y);
							if(PositionEvent != null)
							{
                                PositionEvent.Invoke(this, new PositionEventArgs(pos));
                            }
						break;
                    }
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		public void TCPSendMessage(string message)
		{
            MessagePacket packet = new MessagePacket(message);
            string data = packet.ToJson();
            m_StreamWriter.WriteLine(data);
            m_StreamWriter.Flush();
        }

		public void Login()
		{
			LoginPacket packet = new LoginPacket();
			string data = packet.ToJson();
			m_StreamWriter.WriteLine(data);
			m_StreamWriter.Flush();
		}

		public void SendPosition(Vector2 pos)
		{
			PositionPacket packet = new PositionPacket(pos.X, pos.Y);
            string data = packet.ToJson();
            m_StreamWriter.WriteLine(data);
            m_StreamWriter.Flush();
        }
	}
}
