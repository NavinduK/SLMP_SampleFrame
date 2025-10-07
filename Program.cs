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
            var plcReaderD = new PlcReaderD(plcConnection);

            try
            {
                Console.WriteLine($"Connecting to PLC at {ipAddress}:6000\n");
                plcConnection.Connect(ipAddress, 6000);
                Console.WriteLine("Connected.\n");

                // Run full W4000 test sequence
                plcReader.TestW4000BitSequence();

                // Test D4000 instead of W4000
                // plcReaderD.TestD4000BitSequence();
                
                // Optional: Monitor D4000 for changes
                // Console.WriteLine("\nPress 'M' to monitor D4000 continuously, or any other key to exit...");
                // var key = Console.ReadKey();
                // if (key.KeyChar == 'M' || key.KeyChar == 'm')
                // {
                //     plcReaderD.MonitorD4000();
                // }
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
