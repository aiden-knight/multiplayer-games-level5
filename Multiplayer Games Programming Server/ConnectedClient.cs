﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Server
{
	internal class ConnectedClient
	{
		Socket m_socket;
        StreamReader m_reader;
        StreamWriter m_writer;
        public int m_ID { get; private set; }
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

		public string Read()
		{
            try
            {
                string? packetJSON = string.Empty;
                while ((packetJSON = m_reader.ReadLine()) != null)
                {
                    return packetJSON;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return string.Empty;
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
	}
}
