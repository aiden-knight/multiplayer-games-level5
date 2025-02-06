using Multiplayer_Games_Programming_Packet_Library;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiplayer_Games_Programming_Server
{
    internal class Lobby
    {
        readonly ConnectedClient?[] m_clients;
        readonly int m_maxSize;
        int m_clientCount;
        int m_currentFreeIndex;
        bool m_playing = false;
        int m_host = 0;

        public Lobby(int maxSize)
        {
            m_maxSize = maxSize;
            m_clients = new ConnectedClient[maxSize];
            m_currentFreeIndex = 0;
            m_clientCount = 0;
        }

        public bool IsFull()
        {
            return m_clientCount == m_maxSize;
        }

        public bool IsEmpty()
        {
            return m_clientCount == 0;
        }

        public void AddClient(ConnectedClient client)
        {
            m_clients[m_currentFreeIndex++] = client;
            m_clientCount++;
            // @TODO update clients
        }

        public void SetPlaying(bool playing)
        {
            m_playing = playing;
        }

        public bool RemoveClient(ConnectedClient client)
        {
            for (int i = 0; i < m_maxSize; i++)
            {
                if (m_clients[i] == null) continue;

                if (m_clients[i]!.ID == client.ID)
                {
                    m_clients[i] = null;
                    m_currentFreeIndex = i;
                    m_clientCount--;

                    SendAll(new PlayerLeftPacket());
                    if (m_clientCount != 0 && m_host == i)
                    {
                        for (int j = 0; j < m_maxSize; j++)
                        {
                            if (m_clients[j] == null) continue;

                            m_host = j;
                            break;
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        public void SendReady()
        {
            for (int i = 0; i < m_maxSize; i++)
            {
                m_clients[i]?.SendPacket(new GameReadyPacket(i, m_host));
            }
        }

        public void SendAll(Packet packet)
        {
            for (int i = 0; i < m_maxSize; i++)
            {
                m_clients[i]?.SendPacket(packet);
            }
        }

        public void SendOthers(Packet packet, int senderID)
        {
            for (int i = 0; i < m_maxSize; i++)
            {
                if (m_clients[i] == null) continue;
                if (m_clients[i]!.ID == senderID) continue;
                
                m_clients[i]!.SendPacket(packet);
            }
        }
    }
}
