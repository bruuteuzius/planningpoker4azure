﻿using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Duracellko.PlanningPoker.Client.Test.MockSignalR
{
    internal class MockConnectionFactory : IConnectionFactory
    {
        private readonly InMemoryTransport _transport;

        public MockConnectionFactory(InMemoryTransport transport)
        {
            _transport = transport;
        }

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            _transport.OpenChannel();
            return new ValueTask<ConnectionContext>(new MockConnectionContext(_transport));
        }
    }
}
