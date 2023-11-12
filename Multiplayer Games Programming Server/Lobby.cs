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
        ConcurrentBag<ConnectedClient> m_clients;
        int m_maxSize;

        public Lobby(int maxSize)
        {
            m_maxSize = maxSize;
            m_clients = new ConcurrentBag<ConnectedClient>();
        }

        public bool IsFull()
        {
            return m_clients.Count == m_maxSize;
        }

        public void AddClient(ConnectedClient client)
        {
            m_clients.Add(client);
            // @TODO update other clients
        }
    }
}
