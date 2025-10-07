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

                // 🔹 Test working D200 read first
                Console.WriteLine("=== Testing D200 (Known Working) ===");
                try
                {
                    int d200Value = plcReader.ReadD200();
                    Console.WriteLine($"D200 read successful: {d200Value}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"D200 read failed: {ex.Message}\n");
                }

                // 🔹 Analyze W4000 bit by bit
                Console.WriteLine("=== Analyzing W4000 Bits ===");
                plcReader.AnalyzeW4000Bits();

                // 🔹 Test M (bit) registers - common for heartbeats
                Console.WriteLine("\n=== Testing M (Bit) Registers ===");
                int[] mAddresses = { 0, 1, 8000, 8001, 8002, 8010, 8100 };
                foreach (int addr in mAddresses)
                {
                    try
                    {
                        bool value = plcReader.ReadSingleM(addr);
                        Console.WriteLine($"M{addr}: {(value ? "ON " : "OFF")}");
                        if (value) Console.WriteLine($"  *** M{addr} IS ON - Could be heartbeat! ***");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"M{addr} error: {ex.Message}");
                    }
                }

                // 🔹 Test different W addresses around 4000
                Console.WriteLine("\n=== Testing W Registers Around 4000 ===");
                for (int addr = 3995; addr <= 4005; addr++)
                {
                    try
                    {
                        ushort value = plcReader.ReadSingleW(addr);
                        if (value != 0)
                        {
                            Console.WriteLine($"*** W{addr}: {value:X4} ({value}) - NON-ZERO! ***");
                            
                            // Check all bits if non-zero
                            for (int bit = 0; bit < 16; bit++)
                            {
                                bool bitValue = ((value >> bit) & 1) == 1;
                                if (bitValue) Console.WriteLine($"    Bit {bit}: ON");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"W{addr} error: {ex.Message}");
                    }
                }

                // 🔹 Test writing to W4000 to see if we can control it
                Console.WriteLine("\n=== Testing W4000 Write ===");
                try
                {
                    Console.WriteLine("Writing 0x0001 to W4000...");
                    plcReader.WriteW4000(0x0001);
                    System.Threading.Thread.Sleep(500);
                    
                    Console.WriteLine("Reading W4000 after write...");
                    ushort newValue = plcReader.ReadSingleW(4000);
                    
                    bool heartbeat = (newValue & 0x0001) != 0;
                    Console.WriteLine($"Heartbeat after write: {(heartbeat ? "ON" : "OFF")}");
                    
                    if (heartbeat)
                    {
                        Console.WriteLine("*** SUCCESS! W4000 bit 0 can be controlled manually! ***");
                        Console.WriteLine("*** Ask PLC programmer to check if heartbeat is being written by PLC program ***");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Write test failed: {ex.Message}");
                }

                Console.WriteLine("\n=== RECOMMENDATION ===");
                Console.WriteLine("Please ask your PLC programmer:");
                Console.WriteLine("1. What is the EXACT address of the heartbeat? (M8000, W4000, D1000, etc.)");
                Console.WriteLine("2. Which bit position? (bit 0, 1, 2, etc.)");
                Console.WriteLine("3. Is the heartbeat program actually running?");
                Console.WriteLine("4. Can they manually set the heartbeat ON to test?");

                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
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
