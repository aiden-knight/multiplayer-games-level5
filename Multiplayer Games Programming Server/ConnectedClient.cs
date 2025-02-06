using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;
using System.Security.Cryptography;

namespace Multiplayer_Games_Programming_Server
{
	internal class ConnectedClient
	{
        public int ID { get; private set; }
        public IPEndPoint? UdpEndPoint { get; private set; }

        public Lobby? m_lobby;

		readonly Socket m_socket;
        readonly StreamReader m_reader;
        readonly StreamWriter m_writer;

        public RSAParameters ClientPublicKey { get; private set; }

        public ConnectedClient(Socket socket, int ID)
		{
			m_socket = socket;
            this.ID = ID;

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
            try
            {
                return m_reader.ReadLine();
            }
            catch
            {
                return null;
            }   
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

        public void SendPacketUdp(UdpClient udpListener, Packet packet)
        {
            string data = packet.ToJson();
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            udpListener.SendAsync(bytes, bytes.Length, UdpEndPoint);
        }
        
        public void SetPublicKey(RSAParameters publicKey)
        {
            ClientPublicKey = publicKey;
        }
        public void SetEndPoint(IPEndPoint endPoint)
        {
            UdpEndPoint = endPoint;
        }
    }
}
