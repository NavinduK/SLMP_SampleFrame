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
            var plcReaderW = new PlcReaderW(plcConnection);
            var plcReaderD = new PlcReaderD(plcConnection);

            try
            {
                Console.WriteLine($"Connecting to PLC at {ipAddress}:6000\n");
                plcConnection.Connect(ipAddress, 6000);
                Console.WriteLine("Connected.\n");

                // plcReaderW.TestWBitSequence();
                // plcReaderD.TestDBitSequence();

                for (int i = 0; i < 30; i++)
                {
                    plcReaderW.TestWBitSequence();
                    // plcReaderD.TestDBitSequence();
                    System.Threading.Thread.Sleep(500);
                }
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
