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
        ConnectedClient?[] m_clients;
        int m_maxSize;
        int m_clientCount;
        int m_currentFreeIndex;

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

        public bool RemoveClient(ConnectedClient client)
        {
            for(int i = 0; i < m_maxSize; i++)
            {
                if (m_clients[i] == null) continue;

                if (m_clients[i].m_ID == client.m_ID)
                {
                    m_clients[i] = null;
                    m_currentFreeIndex = i;
                    m_clientCount--;
                    return true;
                }
            }
            return false;
        }
    }
}
