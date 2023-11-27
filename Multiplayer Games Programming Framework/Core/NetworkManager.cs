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
using System.Security.Cryptography;

namespace Multiplayer_Games_Programming_Framework.Core
{
	internal class NetworkManager
	{
		private static NetworkManager m_Instance;

		public static NetworkManager Instance
		{
			get
			{
				if (m_Instance == null)
				{
					return m_Instance = new NetworkManager();
				}
			
				return m_Instance;
			}
		}

		readonly RSACryptoServiceProvider m_RsaProvider;
		RSAParameters m_PublicKey;
		RSAParameters m_PrivateKey;
		RSAParameters m_ServerPublicKey;
		
		readonly TcpClient m_TcpClient;
        readonly UdpClient m_UdpClient;
		bool m_UdpHandshakeCompleted = false;

		NetworkStream m_Stream;
		StreamReader m_StreamReader;
		StreamWriter m_StreamWriter;
		int m_clientID;
		
        public bool InLobby { get; private set; }
        public bool CanPlay { get; private set; }
        int m_playerID;
        public bool PlayerOne { get; private set; }

		public Dictionary<int, Action<Vector2>> PositionActions;
		public Dictionary<int, Action<Vector2, float>> PaddleActions;
		public Action<Vector2, Vector2> BallAction;
		public Action PlayAction;
        public Action<bool> EnablePlay;
        public Action<int> ScoreAction;
        public Action PlayerLeft;

		NetworkManager()
		{
			m_RsaProvider = new RSACryptoServiceProvider(1024);
            m_PublicKey = m_RsaProvider.ExportParameters(false);
            m_PrivateKey = m_RsaProvider.ExportParameters(true);

            m_TcpClient = new TcpClient();
			m_UdpClient = new UdpClient();

            m_clientID = -1;
			PositionActions = new Dictionary<int, Action<Vector2>>();
            PaddleActions = new Dictionary<int, Action<Vector2, float>>();
		}

        public bool Connected()
        {
            return m_TcpClient.Connected;
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
			_ = UdpProcessServerResponse();

            Thread tcpThread = new Thread(new ThreadStart(TcpProcessServerResponse))
            {
                Name = "TCP THREAD"
            };
            tcpThread.Start();

        }

		void ConnectionClosed()
		{
			m_TcpClient.Close();
            m_UdpClient.Close();
        }

		void HandlePacket(Packet? p)
		{
            if (p == null) return;

            PacketType type = p.Type;
            if (type == PacketType.ENCRYPTED)
            {
                byte[] decrypted;
                EncryptedPacket encryptedPacket = (EncryptedPacket)p;

                lock (m_RsaProvider)
                {
                    m_RsaProvider.ImportParameters(m_PrivateKey);
                    decrypted = m_RsaProvider.Decrypt(encryptedPacket.encryptedPacket, false);
                }
                string decryptedJSON = Encoding.UTF8.GetString(decrypted);

                // Set packet and types to new packet
                p = Packet.Deserialize(decryptedJSON);
                if (p == null) return;

                type = p.Type;
            }

            switch (type)
            {
                case PacketType.MESSAGE:
                    string message = ((MessagePacket)p).message;
                    Debug.WriteLine(message);
                break;
                case PacketType.GAME_READY:
                    GameReadyPacket gameReadyPacket = (GameReadyPacket)p;
                    m_playerID = gameReadyPacket.playerID;
                    if (m_playerID == gameReadyPacket.hostID)
                    {
                        CanPlay = true;
                        PlayerOne = true;
                        EnablePlay?.Invoke(CanPlay);
                    }
                    else
                    {
                        PlayerOne = false;
                    }
                break;
                case PacketType.POSITION:
                    PositionPacket posPacket = (PositionPacket)p;
                    Vector2 pos = new Vector2(posPacket.x, posPacket.y);
                    if (PositionActions.ContainsKey(0))
                        PositionActions[0]?.Invoke(pos);
                break;
                case PacketType.PADDLE:
                    PaddlePacket paddlePacket = (PaddlePacket)p;
                    if (PaddleActions.ContainsKey(1))
                        PaddleActions[1]?.Invoke(new Vector2(paddlePacket.x, paddlePacket.y), paddlePacket.input);
                break;
                case PacketType.BALL:
                    BallPacket ballPacket = (BallPacket)p;
                    Vector2 ballPos = new Vector2(ballPacket.x, ballPacket.y);
                    Vector2 ballVelocity = new Vector2(ballPacket.vX, ballPacket.vY);
                    BallAction?.Invoke(ballPos, ballVelocity);
                break;
                case PacketType.SCORE:
                    ScorePacket scorePacket = (ScorePacket)p;
                    ScoreAction?.Invoke(scorePacket.score);
                break;
                case PacketType.PLAY:
                    PlayAction?.Invoke();
                break;
                case PacketType.JOIN_LOBBY:
                    InLobby = true;
                break;
                case PacketType.PLAYER_LEFT:
                    CanPlay = false;
                    EnablePlay?.Invoke(CanPlay);
                    PlayerLeft?.Invoke();
                break;
                case PacketType.LOGIN:
                    LoginPacket loginPacket = (LoginPacket)p;
                    m_clientID = loginPacket.ID;
                    m_ServerPublicKey = loginPacket.publicKey;
                    SendPacketEncrypted(new MessagePacket("Test Encrypt"));
                    SendPacketUdp(new UdpLoginPacket(m_clientID));
                break;
                case PacketType.UDP_LOGIN:
                    m_UdpHandshakeCompleted = true;
                    Debug.WriteLine("UDP Handshake Completed");
                break;
            }
        }

		void TcpProcessServerResponse()
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
				HandlePacket(p);
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
                    HandlePacket(p);
                }
            }
            catch (SocketException e)
            {
                Debug.WriteLine("Client UDP Read Method exception: " + e.Message);
            }
        }

		public void SendPacketEncrypted(Packet packet, bool udp = false)
		{
            byte[] encryptedJSON;

            lock (m_RsaProvider)
            {
                m_RsaProvider.ImportParameters(m_ServerPublicKey);
                string json = packet.ToJson();
                encryptedJSON = m_RsaProvider.Encrypt(Encoding.UTF8.GetBytes(json), false);
            }

            EncryptedPacket encryptedPacket = new EncryptedPacket(encryptedJSON);
            if (udp)
            {
                SendPacketUdp(encryptedPacket);
            }
            else
            {
                SendPacket(encryptedPacket);
            }
        }

		public void SendPacket(Packet packet)
		{
            if (!m_TcpClient.Connected) return;

            string data = packet.ToJson();
            m_StreamWriter.WriteLine(data);
            m_StreamWriter.Flush();
        }

		public void SendPacketUdp(Packet packet)
		{
            if (!m_TcpClient.Connected) return;

            if (packet.Type != PacketType.UDP_LOGIN && !m_UdpHandshakeCompleted)
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
			SendPacket(new LoginPacket(-1, m_PublicKey));
		}
	}
}
