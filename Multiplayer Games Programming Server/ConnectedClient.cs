﻿using System.Net.Sockets;
using System.Text;
using Multiplayer_Games_Programming_Packet_Library;

namespace Multiplayer_Games_Programming_Server
{
	internal class ConnectedClient
	{
		public Socket m_socket;
		public ConnectedClient(Socket socket)
		{
			m_socket = socket;
		}

		public void Close()
		{
			
		}

		//public string Read()
		//{
			
		//}

		//public void Send(string message)
		//{
		//}
	}
}
