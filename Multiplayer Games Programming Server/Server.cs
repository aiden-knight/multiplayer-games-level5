using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;
using System.Xml;

namespace Multiplayer_Games_Programming_Server
{
	internal class Server
	{
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

		private void ClientMethod(int ID)
		{
            while (m_running)
			{
				string? json = m_Clients[ID].Read();
				if (json == null) break; // connectioned closed results in null packet

                Packet? p = Packet.Deserialize(json);

				if (p == null) continue;

				PacketType type = p.m_Type;
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
						LoginPacket loginPacket = new LoginPacket(ID);
						m_Clients[ID].SendPacket(loginPacket);
					break;
					case PacketType.POSITION:
						PositionPacket posPacket = (PositionPacket)p;
                        m_Clients[ID].m_lobby?.SendOthers(posPacket, ID);
                        break;
                    case PacketType.PLAY:
						m_Clients[ID].m_lobby?.SendAll(p);
                    break;
                    case PacketType.JOIN_LOBBY:
                        lock(m_LobbyLock)
						{
							bool foundLobby = false;
							foreach (Lobby lobby in m_Lobbies)
							{
								if (lobby.IsFull()) continue;
								foundLobby = true;

								lobby.AddClient(m_Clients[ID]);
								m_Clients[ID].m_lobby = lobby;
								if(lobby.IsFull())
								{
									lobby.SendReady();
                                }
							}

							if(!foundLobby)
							{
								Lobby lobby = new Lobby(2);
                                m_Lobbies.Add(lobby);

								lobby.AddClient(m_Clients[ID]);
                                m_Clients[ID].m_lobby = lobby;
							}
						} // end lock
                    break;
                }
            }

			ConnectedClient? disconnectedClient;
			m_Clients.TryRemove(ID, out disconnectedClient);

			if (disconnectedClient == null) return;

			m_UdpPortToClient.TryRemove(disconnectedClient.m_udpEndPoint.Port, out _);

			if(disconnectedClient.m_lobby != null)
			{
				Lobby lobby = disconnectedClient.m_lobby;
                lock (m_LobbyLock)
                {
					lobby.RemoveClient(disconnectedClient);
					if(lobby.IsEmpty())
						m_Lobbies.Remove(lobby);
                }
            }
			
            disconnectedClient.Close();
			lock(m_ConsoleLock)
			{
                Console.WriteLine("Connection terminated with ID: {0}", ID);
            }
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
				ConnectedClient client;
				if(m_UdpPortToClient.ContainsKey(port)) client = m_UdpPortToClient[port];

				PacketType type = p.m_Type;
				switch (type)
				{
					case PacketType.LOGIN:
                        LoginPacket loginPacket = (LoginPacket)p;
						client = m_Clients[loginPacket.ID];

                        m_UdpPortToClient[port] = client;
						client.SetEndPoint(receiveResult.RemoteEndPoint);
                        client.SendPacketUdp(m_UdpListener, loginPacket);
                    break;
				}
            }

            m_UdpListener.Close();
        }
    }
}
