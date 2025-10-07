using System;
using System.Net;
using SLMP_SampleFrame.Services;

namespace SLMP_SampleFrame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Program...");

            byte[] byAdres = { 10, 114, 41, 69 };
            IPAddress ipAddress = new IPAddress(byAdres);

            var plcConnection = new PlcConnection();
            var plcReader = new PlcReader(plcConnection);

            try
            {
                Console.WriteLine($"Connecting to PLC at {ipAddress}:6000\n");
                plcConnection.Connect(ipAddress, 6000);
                Console.WriteLine("Connected. Testing communication...\n");

                // 🔹 Analyze W4000 bit by bit
                Console.WriteLine("=== Analyzing W4000 Bits ===");
                plcReader.AnalyzeW4000Bits();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
            }
            finally
            {
                plcConnection.Dispose();
            }
        }
    }
}
