using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace TcpListeningService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TcpListeningService : StatelessService
    {
        public TcpListeningService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            var endpoints = Context.CodePackageActivationContext.GetEndpoints()
                .Where(endpoint => endpoint.Protocol == EndpointProtocol.Tcp)
                .Select(endpoint => endpoint.Name);

            return endpoints.Select(endpoint => new ServiceInstanceListener(
                serviceContext => new TcpCommunicationListener(serviceContext, ServiceEventSource.Current, endpoint), endpoint));
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            long iterations = 0;

            TcpClient client;

            NetworkStream stream = null;

            var ipEndPoint = new IPEndPoint(IPAddress.Any, 5678);
            var listener = new TcpListener(ipEndPoint) { ExclusiveAddressUse = false };
            while (true)
            {
                if (stream == null)
                {
                    listener.Start();
                }

                client = await listener.AcceptTcpClientAsync();

                cancellationToken.ThrowIfCancellationRequested();
                ServiceEventSource.Current.ServiceMessage(this.Context, "working-{0}", ++iterations);

                try
                {
                    ThreadPool.QueueUserWorkItem(ThreadProc, client);
                }   
                catch (System.Exception)
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }

                    client.Close();
                    listener.Stop();
                }
                finally
                {

                }
            }
        }

        private static async void ThreadProc(object obj)
        {
            var client = (TcpClient)obj;
            byte[] bytes = new byte[2048];
            string data = null;

            try
            {
                NetworkStream stream = client.GetStream();

                int i;

                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    string commandToSend = string.Empty;
                    Trace.TraceInformation($"Received: {data}");

                    byte[] toReturn = Encoding.ASCII.GetBytes(data);
                    await stream.WriteAsync(toReturn, 0, toReturn.Length);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
