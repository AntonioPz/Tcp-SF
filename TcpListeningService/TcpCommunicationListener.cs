using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Fabric;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpListeningService
{
    internal class TcpCommunicationListener : ICommunicationListener
    {
        private StatelessServiceContext serviceContext;

        private int port;

        public TcpCommunicationListener(StatelessServiceContext serviceContext, ServiceEventSource current, string endpoint)
        {
            this.serviceContext = serviceContext;
            port = serviceContext.CodePackageActivationContext.GetEndpoint("TCPListenerEndpoint").Port;
        }
        public void Abort()
        {
        }


        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(serviceContext, "Starting TCP listener on port {0}", port);
            string uriPublished = $"tcp:{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:{port}";

            ServiceEventSource.Current.ServiceMessage(serviceContext, "TCP listener started and published with address {0}", uriPublished);

            return Task.FromResult(uriPublished);
        }
        private void StopTcpListener()
        {
            ServiceEventSource.Current.ServiceMessage(serviceContext, "Stopping TCP listener on port {0}", port);

            ServiceEventSource.Current.ServiceMessage(serviceContext, "TCP listener stopped.");
        }
    }
}