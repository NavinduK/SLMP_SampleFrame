using System;
using System.Net.Sockets;

namespace SLMP_SampleFrame.Services
{
    public class PlcReaderD : PlcConnection
    {
        private readonly PlcConnection _connection;

        public PlcReaderD(PlcConnection connection)
        {
            _connection = connection;
        }

        // ===============================
        // Read single D-word (16 bits)
        // ===============================
        public ushort ReadSingleD(int address)
        {
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00,
                0x00,
                (byte)(address & 0xFF),
                (byte)((address >> 8) & 0xFF),
                (byte)((address >> 16) & 0xFF),
                0xA8, // Device code: D register (changed from 0xB4)
                0x01, 0x00 // Read 1 word
            };

            try
            {
                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                byte[] data = new byte[20];
                stream.ReadTimeout = 2000;
                int bytes = stream.Read(data, 0, data.Length);

                if (bytes < 11)
                    throw new Exception("Incomplete response");

                if (data[9] != 0x00 || data[10] != 0x00)
                {
                    ushort errorCode = (ushort)((data[10] << 8) | data[9]);
                    throw new Exception($"PLC error: {errorCode:X4}");
                }

                if (bytes < 13)
                    throw new Exception("Missing data");

                byte low = data[11];
                byte high = data[12];
                ushort value = (ushort)((high << 8) | low);

                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading D{address}: {ex.Message}");
                return 0;
            }
        }

        // ===============================
        // Write single bit (on/off)
        // ===============================
        public void WriteSingleDBit(int address, int bit, bool on)
        {
            // Read current word
            ushort current = ReadSingleD(address);
            ushort newValue;

            if (on)
                newValue = (ushort)(current | (1 << bit));   // Set bit ON
            else
                newValue = (ushort)(current & ~(1 << bit));  // Set bit OFF

            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0E, 0x00, 0x10, 0x00, 0x01, 0x14, 0x00,
                0x00,
                (byte)(address & 0xFF),
                (byte)((address >> 8) & 0xFF),
                (byte)((address >> 16) & 0xFF),
                0xA8, // D device (changed from 0xB4)
                0x01, 0x00,
                (byte)(newValue & 0xFF),
                (byte)((newValue >> 8) & 0xFF)
            };

            try
            {
                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                byte[] data = new byte[20];
                stream.ReadTimeout = 2000;
                int bytes = stream.Read(data, 0, data.Length);

                if (bytes < 11)
                    throw new Exception("Incomplete response");

                if (data[9] != 0x00 || data[10] != 0x00)
                {
                    ushort errorCode = (ushort)((data[10] << 8) | data[9]);
                    throw new Exception($"PLC error: {errorCode:X4}");
                }

                Console.WriteLine($"✅ D{address}.{bit} written {(on ? "ON" : "OFF")} successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing D{address}.{bit}: {ex.Message}");
            }
        }

        // ===============================
        // Print all bits in a D word
        // ===============================
        public void PrintBits(ushort value, int address)
        {
            Console.WriteLine($"\nD{address} = 0x{value:X4} ({value})");
            for (int bit = 0; bit < 16; bit++)
            {
                bool bitValue = ((value >> bit) & 1) == 1;
                Console.WriteLine($"  Bit {bit,2}: {(bitValue ? "ON " : "OFF")}");
            }
        }

        // ===============================
        // Full test sequence for D
        // ===============================
        public void TestDBitSequence()
        {
            int address = 4000;
            Console.WriteLine("=== Step 1: Read all bits from D" + address + " ===");
            ushort before = ReadSingleD(address);
            PrintBits(before, address);

            // Console.WriteLine("\n=== Step 2: Write D" + address + ".0 = ON ===");
            // WriteSingleDBit(address, 0, true);

            // ushort afterOn = ReadSingleD(address);
            // PrintBits(afterOn, address);

            // Console.WriteLine("\n=== Step 3: Write D" + address + ".0 = OFF ===");
            // WriteSingleDBit(address, 0, false);

            // ushort afterOff = ReadSingleD(address);
            // PrintBits(afterOff, address);
        }
    }
}
