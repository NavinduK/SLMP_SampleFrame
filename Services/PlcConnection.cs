using System;
using System.Net;
using System.Net.Sockets;

namespace SLMP_SampleFrame.Services
{
    public class PlcConnection
    {
        protected TcpClient tcpClient;

        public PlcConnection()
        {
            tcpClient = new TcpClient();
        }

        public void Connect(IPAddress ip, int port)
        {
            try
            {
                Console.WriteLine("Connecting to the PLC...");
                tcpClient.Connect(ip, port);
                Console.WriteLine($"Connected to PLC at {ip}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex}");
                throw;
            }
        }

        public bool IsConnected => tcpClient?.Connected ?? false;

        public NetworkStream GetStream()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to PLC");
            
            return tcpClient.GetStream();
        }

        public void Reconnect(IPAddress ip, int port)
        {
            try
            {
                Disconnect();
                tcpClient = new TcpClient();
                Connect(ip, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reconnection failed: {ex.Message}");
                throw;
            }
        }

        public void Disconnect()
        {
            tcpClient?.Close();
        }

        public void Dispose()
        {
            tcpClient?.Dispose();
        }
    }
}