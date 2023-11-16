using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;
using System.Xml;
using System.Security.Cryptography;

namespace Multiplayer_Games_Programming_Server
{
	internal class Server
	{
		RSACryptoServiceProvider m_RsaProvider;
		RSAParameters m_PublicKey;
		RSAParameters m_PrivateKey;

		TcpListener m_TcpListener;
		UdpClient m_UdpListener;

		int m_ID;
		ConcurrentDictionary<int, ConnectedClient> m_Clients;
		ConcurrentDictionary<int, ConnectedClient> m_UdpPortToClient;
		bool m_running;

		object m_ConsoleLock = new object();

		object m_LobbyLock = new object();
		List<Lobby> m_Lobbies = new List<Lobby>();

		public Server(string ipAddress, int port)
		{
			m_RsaProvider = new RSACryptoServiceProvider(1024);
			m_PublicKey = m_RsaProvider.ExportParameters(false);
			m_PrivateKey = m_RsaProvider.ExportParameters(true);

			IPAddress ip = IPAddress.Parse(ipAddress);
			m_TcpListener = new TcpListener(ip, port);
			m_UdpListener = new UdpClient(port);

			m_Clients = new ConcurrentDictionary<int, ConnectedClient>();
            m_UdpPortToClient = new ConcurrentDictionary<int, ConnectedClient>();
			m_ID = 0;
			m_running = false;
		}

		public void Start()
		{
			try
			{
                m_TcpListener.Start();
                Console.WriteLine("Server Started....");
				m_running = true;
				UDPListen();

				while(m_running)
				{
                    Socket socket = m_TcpListener.AcceptSocket();
                    int currentID = m_ID++;

                    ConnectedClient client = new ConnectedClient(socket, currentID);

                    lock (m_ConsoleLock)
                    {
                        Console.WriteLine("Connection made with ID: {0}", currentID);
                    }

                    m_Clients.TryAdd(currentID, client);
                    Thread clientThread = new Thread(() => ClientMethod(currentID));
                    clientThread.Name = string.Format("ClientThread ID: {0}", currentID);
                    clientThread.Start();
                }
            }
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
        }

		public void Stop()
		{
			m_running = false;
			
			m_TcpListener.Stop();
			m_UdpListener.Close();
		}

		void SendPacketEncrypted(ConnectedClient client, Packet packet, bool udp = false)
		{
			byte[] encryptedJSON;

            lock (m_RsaProvider)
			{
				m_RsaProvider.ImportParameters(client.m_clientPublicKey);
				string json = packet.ToJson();
                encryptedJSON = m_RsaProvider.Encrypt(Encoding.UTF8.GetBytes(json), false);
			}

            EncryptedPacket encryptedPacket = new EncryptedPacket(encryptedJSON);
            if (udp)
            {
                client.SendPacketUdp(m_UdpListener, encryptedPacket);
            }
            else
            {
                client.SendPacket(encryptedPacket);
            }
		}

		void HandlePacket(ConnectedClient client, Packet? p)
		{
            if (p == null) return;
            PacketType type = p.m_Type;

            // Extra check for encrypted packet
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

                type = p.m_Type;
            }

            switch (type)
            {
                case PacketType.MESSAGE:
                    string message = ((MessagePacket)p).message;

                    lock (m_ConsoleLock)
                    {
                        Console.WriteLine(message);
                    }
                    break;
                case PacketType.LOGIN:
                    client.SetPublicKey(((LoginPacket)p).publicKey);

                    client.SendPacket(new LoginPacket(client.m_ID, m_PublicKey));
                    break;
                case PacketType.POSITION:
                    PositionPacket posPacket = (PositionPacket)p;
                    client.m_lobby?.SendOthers(posPacket, client.m_ID);
                    break;
                case PacketType.BALL:
                    BallPacket ballPacket = (BallPacket)p;
                    client.m_lobby?.SendOthers(ballPacket, client.m_ID);
                    break;
                case PacketType.PLAY:
                    client.m_lobby?.SendAll(p);
                    break;
                case PacketType.JOIN_LOBBY:
                    lock (m_LobbyLock)
                    {
                        bool foundLobby = false;
                        foreach (Lobby lobby in m_Lobbies)
                        {
                            if (lobby.IsFull()) continue;
                            foundLobby = true;

                            lobby.AddClient(client);
                            client.m_lobby = lobby;
                            if (lobby.IsFull())
                            {
                                lobby.SendReady();
                            }
                        }

                        if (!foundLobby)
                        {
                            Lobby lobby = new Lobby(2);
                            m_Lobbies.Add(lobby);

                            lobby.AddClient(client);
                            client.m_lobby = lobby;
                        }
                    } // end lock
                    break;
            }
        }

		void ClientMethod(int ID)
		{
            while (m_running)
			{
				string? json = m_Clients[ID].Read();
				if (json == null) break; // connectioned closed results in null packet

                Packet? p = Packet.Deserialize(json);

                HandlePacket(m_Clients[ID], p);
            }

            RemoveClient(ID);
		}

        async Task UDPListen()
        {
            while (m_running)
            {
                UdpReceiveResult receiveResult = await m_UdpListener.ReceiveAsync();
                byte[] receivedData = receiveResult.Buffer;

                string packetJSON = Encoding.UTF8.GetString(receivedData, 0, receivedData.Length);
                
				Packet? p = Packet.Deserialize(packetJSON);
				if (p == null) continue;

				int port = receiveResult.RemoteEndPoint.Port;
                if (m_UdpPortToClient.ContainsKey(port))
                {
                    HandlePacket(m_UdpPortToClient[port], p);
                }
                else
                {
                    PacketType type = p.m_Type;
                    if(type == PacketType.UDP_LOGIN)
                    {
                        UdpLoginPacket loginPacket = (UdpLoginPacket)p;
                        ConnectedClient client = m_Clients[loginPacket.ID];

                        m_UdpPortToClient[port] = client;
                        client.SetEndPoint(receiveResult.RemoteEndPoint);
                        client.SendPacketUdp(m_UdpListener, loginPacket);
                    }
                }
            }

            m_UdpListener.Close();
        }

        void RemoveClient(int clientID)
        {
            // as connection closed handle removing of client data
            ConnectedClient? disconnectedClient;
            m_Clients.TryRemove(clientID, out disconnectedClient);

            if (disconnectedClient == null) return;
            m_UdpPortToClient.TryRemove(disconnectedClient.m_udpEndPoint.Port, out _);

            // if client was in lobby disconnect them
            if (disconnectedClient.m_lobby != null)
            {
                Lobby lobby = disconnectedClient.m_lobby;
                lock (m_LobbyLock)
                {
                    lobby.RemoveClient(disconnectedClient);
                    if (lobby.IsEmpty())
                        m_Lobbies.Remove(lobby);
                }
            }

            // close up disconnected client
            disconnectedClient.Close();
            lock (m_ConsoleLock)
            {
                Console.WriteLine("Connection terminated with ID: {0}", clientID);
            }
        }
    }
}
