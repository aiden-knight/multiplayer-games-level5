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

					if(m_Clients.Count == 2)
					{
						foreach(ConnectedClient connectedClient in m_Clients.Values)
						{
							GameReadyPacket packet = new GameReadyPacket();
							connectedClient.SendPacket(packet);
						}
					}
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
		}

		private void ClientMethod(int ID)
		{
            while (m_running)
			{
                string packetJSON = m_Clients[ID].Read();

                Packet? p = Packet.Deserialize(packetJSON);
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
						foreach(ConnectedClient client in m_Clients.Values)
						{
							if(client.m_ID == ID)continue;
							client.SendPacket(posPacket);
						}
					break;
				}
            }
			
			m_Clients[ID].Close();
			m_Clients.TryRemove(ID, out _);
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
