using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Server
{
	internal class Server
	{
		TcpListener m_TcpListener;

		int m_ID;
		ConcurrentDictionary<int, ConnectedClient> m_Clients;
		object m_ConsoleLock = new object();

		public Server(string ipAddress, int port)
		{
			IPAddress ip = IPAddress.Parse(ipAddress);
			m_TcpListener = new TcpListener(ip, port);

			m_Clients = new ConcurrentDictionary<int, ConnectedClient>();
			m_ID = 0;
		}

		public void Start()
		{
			try
			{
                m_TcpListener.Start();
                Console.WriteLine("Server Started....");

				while(true)
				{
                    Socket socket = m_TcpListener.AcceptSocket();
                    ConnectedClient client = new ConnectedClient(socket);

                    int currentID = m_ID++;
                    Console.WriteLine("Connection made with ID: {0}", currentID);

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
			m_TcpListener.Stop();
		}

		private void ClientMethod(int index)
		{
			string packetJSON = m_Clients[index].Read();

			Packet? p = Packet.Deserialize(packetJSON);
			if (p != null)
			{
				if(p.m_Type == PacketType.MESSAGE)
				{
					string message = ((MessagePacket)p).message;

                    lock (m_ConsoleLock)
                    {
                        Console.WriteLine(message);
                    }
                }
			}

			m_Clients[index].Send(string.Format("You successfully logend in with ID: {0}",index));

			while (true) ;
			m_Clients[index].Close();
			m_Clients.TryRemove(index, out _);
		}
	}
}
