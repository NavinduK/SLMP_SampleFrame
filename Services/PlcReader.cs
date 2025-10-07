using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SLMP_SampleFrame.Services
{
    public class PlcReader : PlcConnection
    {
        private readonly PlcConnection _connection;

        public PlcReader(PlcConnection connection)
        {
            _connection = connection;
        }

        // Test method to read a single W register
        public ushort ReadSingleW(int address)
        {
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00,
                0x00,
                (byte)(address & 0xFF), (byte)((address >> 8) & 0xFF), (byte)((address >> 16) & 0xFF),
                0xB4, // W device
                0x01, 0x00
            };

            try
            {
                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                byte[] data = new byte[20];
                stream.ReadTimeout = 2000;
                int bytes = stream.Read(data, 0, data.Length);

                if (bytes < 11) throw new Exception("Incomplete response");

                if (data[9] != 0x00 || data[10] != 0x00)
                {
                    ushort errorCode = (ushort)((data[10] << 8) | data[9]);
                    throw new Exception($"PLC error: {errorCode:X4}");
                }

                if (bytes < 13) throw new Exception("Missing data");

                byte low = data[11];
                byte high = data[12];
                ushort value = (ushort)((high << 8) | low);

                Console.WriteLine($"W{address}: {value:X4} ({value})");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading W{address}: {ex.Message}");
                return 0;
            }
        }

        // Check all bits in W4000 to see if heartbeat is in a different bit position
        public void AnalyzeW4000Bits()
        {
            try
            {
                ushort w4000 = ReadSingleW(4000);
                Console.WriteLine($"\nW4000 Bit Analysis: {w4000:X4} ({w4000})");
                
                for (int bit = 0; bit < 16; bit++)
                {
                    bool bitValue = ((w4000 >> bit) & 1) == 1;
                    Console.WriteLine($"  Bit {bit,2}: {(bitValue ? "ON " : "OFF")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing W4000: {ex.Message}");
            }
        }
    }
}
