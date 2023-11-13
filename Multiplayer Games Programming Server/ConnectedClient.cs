using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Server
{
	internal class ConnectedClient
	{
        public int m_ID { get; private set; }
        public IPEndPoint m_udpEndPoint { get; private set; }

        public Lobby? m_lobby;

		Socket m_socket;
        StreamReader m_reader;
        StreamWriter m_writer;
        public ConnectedClient(Socket socket, int ID)
		{
			m_socket = socket;
            m_ID = ID;

			NetworkStream stream = new NetworkStream(m_socket, false);
            m_reader = new StreamReader(stream, Encoding.UTF8);
            m_writer = new StreamWriter(stream, Encoding.UTF8);
        }

		public void Close()
		{
            m_socket.Close();
        }

		public string? Read()
		{
            return m_reader.ReadLine();
        }
        public void Send(string message)
		{
            try
            {
                MessagePacket packet = new MessagePacket(message);
                string data = packet.ToJson();
                m_writer.WriteLine(data);
                m_writer.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendPacket(Packet packet)
        {
            try
            {
                string data = packet.ToJson();
                m_writer.WriteLine(data);
                m_writer.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SetEndPoint(IPEndPoint endPoint)
        {
            m_udpEndPoint = endPoint;
        }

        public void SendPacketUdp(UdpClient udpListener, Packet packet)
        {
            string data = packet.ToJson();
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            udpListener.SendAsync(bytes, bytes.Length, m_udpEndPoint);
        }
    }
}
