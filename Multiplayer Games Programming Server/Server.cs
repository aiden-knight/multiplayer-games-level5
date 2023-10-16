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

		ConcurrentDictionary<int, ConnectedClient> m_Clients;

		public Server(string ipAddress, int port)
		{
			IPAddress ip = IPAddress.Parse(ipAddress);
			m_TcpListener = new TcpListener(ip, port);

			m_Clients = new ConcurrentDictionary<int, ConnectedClient>();
		}

		public void Start()
		{
			try
			{
                m_TcpListener.Start();
                Console.WriteLine("Server Started....");

                Socket socket = m_TcpListener.AcceptSocket();
                Console.WriteLine("Connection made");
                ConnectedClient client = new ConnectedClient(socket);

                m_Clients.TryAdd(0, client);
                ClientMethod(0);
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
			try
			{
				string message;
				NetworkStream stream = new NetworkStream(m_Clients[index].m_socket, false);
				StreamReader reader = new StreamReader(stream, Encoding.UTF8);
				StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

				while((message = reader.ReadLine()) != null)
				{
					Console.WriteLine("Recieved Message - {0}", message);

					writer.WriteLine("Logged in");
					writer.Flush();
				}
			}
			catch(Exception ex)
			{
                Console.WriteLine(ex.Message);
            }
			finally
			{
                m_Clients[index].m_socket.Close();
            }
		}
	}
}
