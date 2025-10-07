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

        public bool ReadHeartbeat()
        {
            // --- Read W4000 (1 word) - exact same pattern as working D200 ---
            // D200: 0x00, 0xC8, 0x00, 0x00, 0xA8, 0x01, 0x00 
            // W4000: 0xA0, 0x0F, 0x00, 0x00, 0xB4, 0x01, 0x00
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,  // Same header as D200
                0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00,  // Same command as D200
                0x00,                                       // Sub command  
                0xA0, 0x0F, 0x00,                          // W4000 (4000 = 0x0FA0 little endian)
                0xB4,                                       // Device code W
                0x01, 0x00                                  // 1 word
            };

            try
            {
                if (!_connection.IsConnected)
                    throw new InvalidOperationException("Not connected to PLC");

                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                byte[] data = new byte[200];
                stream.ReadTimeout = 2000;  // Increased timeout
                int bytes = stream.Read(data, 0, data.Length);
                
                if (bytes < 11) throw new Exception("Incomplete response - too short");

                // Check for SLMP error in response
                if (data[9] != 0x00 || data[10] != 0x00)
                {
                    ushort errorCode = (ushort)((data[10] << 8) | data[9]);
                    throw new Exception($"PLC returned error: {errorCode:X4}");
                }

                if (bytes < 13) throw new Exception("Incomplete response - missing data");

                byte low = data[11];
                byte high = data[12];
                ushort wordValue = (ushort)((high << 8) | low);

                // Debug: Show the actual value being read
                Console.WriteLine($"W4000 Raw Value: {wordValue:X4} ({wordValue}) - Low:{low:X2}, High:{high:X2}");

                bool heartbeat = (wordValue & 0x0001) != 0;
                return heartbeat;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during read: {ex.Message}");
                return false;
            }
        }

        // ✅ New: Read multiple consecutive words (bulk read)
        public Dictionary<int, ushort> ReadWordsRange(int startAddress, int wordCount)
        {
            // Limit count to avoid very large frames
            if (wordCount <= 0 || wordCount > 256)
                throw new ArgumentOutOfRangeException(nameof(wordCount), "wordCount must be between 1 and 256");

            // Build SLMP 3E frame for read command (0x0401)
            // Using same structure as working D200 read
            byte[] payload = new byte[21];
            payload[0] = 0x50; payload[1] = 0x00;                    // Sub header
            payload[2] = 0x00; payload[3] = 0xff;                    // Network number
            payload[4] = 0xff; payload[5] = 0x03; payload[6] = 0x00; // PC number, Request unit I/O, Request unit station
            payload[7] = 0x0C; payload[8] = 0x00;                    // Request data length (12 bytes)
            payload[9] = 0x10; payload[10] = 0x00;                   // CPU monitoring timer
            payload[11] = 0x01; payload[12] = 0x04; payload[13] = 0x00; // Main command (0x0401 = batch read)
            payload[14] = 0x00;                                      // Sub command

            // Device address (3 bytes) - W register addressing (little endian)
            payload[15] = (byte)(startAddress & 0xFF);
            payload[16] = (byte)((startAddress >> 8) & 0xFF);
            payload[17] = (byte)((startAddress >> 16) & 0xFF);
            payload[18] = 0xB4;  // device code W (0xB4)
            payload[19] = (byte)(wordCount & 0xFF);
            payload[20] = (byte)((wordCount >> 8) & 0xFF);

            try
            {
                if (!_connection.IsConnected)
                    throw new InvalidOperationException("Not connected to PLC");

                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                // Expected bytes: header(11) + count*2
                byte[] data = new byte[1024];
                stream.ReadTimeout = 3000;  // Increased timeout for bulk read
                int bytes = stream.Read(data, 0, data.Length);
                
                if (bytes < 11) throw new Exception("Incomplete response - too short");

                // Check for SLMP error in response
                if (data[9] != 0x00 || data[10] != 0x00)
                {
                    ushort errorCode = (ushort)((data[10] << 8) | data[9]);
                    throw new Exception($"PLC returned error: {errorCode:X4}");
                }

                int expectedDataBytes = wordCount * 2;
                if (bytes < 11 + expectedDataBytes) 
                    throw new Exception($"Incomplete response - expected {11 + expectedDataBytes} bytes, got {bytes}");

                var results = new Dictionary<int, ushort>();
                int offset = 11;
                for (int i = 0; i < wordCount; i++)
                {
                    if (offset + 1 >= bytes) break;
                    ushort value = (ushort)(data[offset] | (data[offset + 1] << 8));
                    results[startAddress + i] = value;
                    offset += 2;
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk read: {ex.Message}");
                return new Dictionary<int, ushort>();
            }
        }

        // Original working D200 read for comparison
        public int ReadD200()
        {
            //Request frame for read D200 register (this was working)
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00,
                0x00, 0xC8, 0x00, 0x00, 0xA8, 0x01, 0x00
            };

            try
            {
                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                byte[] data = new byte[20];
                stream.ReadTimeout = 1000;

                int bytes = stream.Read(data, 0, data.Length);

                if (data[9] == 0 && data[10] == 0)
                {
                    byte lowbyteResponse = data[11];
                    int hibyteResponse = data[12];
                    int afterConversion = (hibyteResponse << 8) + lowbyteResponse;

                    Console.WriteLine("Read D200 finished correctly!");
                    Console.WriteLine($"Read value D200 (HEX): Hi [{hibyteResponse:X}], Low [{lowbyteResponse:X}]");
                    Console.WriteLine($"Read value D200 (DEC): {afterConversion}");

                    return afterConversion;
                }
                else
                {
                    Console.WriteLine("Error in D200 response");
                    throw new InvalidOperationException("PLC returned error response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during D200 read: {ex.Message}");
                throw;
            }
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

        // Read M (bit) registers - these are often used for heartbeats
        public bool ReadSingleM(int address)
        {
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0C, 0x00, 0x10, 0x00, 0x01, 0x04, 0x00,
                0x00,
                (byte)(address & 0xFF), (byte)((address >> 8) & 0xFF), (byte)((address >> 16) & 0xFF),
                0x90, // M device (bit)
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

                if (bytes < 12) throw new Exception("Missing data");

                bool value = (data[11] & 0x01) != 0;
                Console.WriteLine($"M{address}: {(value ? "ON" : "OFF")}");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading M{address}: {ex.Message}");
                return false;
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

        // Write to W4000 to test if we can change its value
        public void WriteW4000(ushort value)
        {
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0E, 0x00, 0x14, 0x00, 0x01, 0x14, 0x00,
                0x00,
                0xA0, 0x0F, 0x00, // W4000 address
                0xB4, // W device
                0x01, 0x00, // 1 word
                (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF)
            };

            try
            {
                NetworkStream stream = _connection.GetStream();
                stream.Write(payload, 0, payload.Length);

                byte[] data = new byte[20];
                stream.ReadTimeout = 2000;
                int bytes = stream.Read(data, 0, data.Length);

                if (bytes >= 11 && data[9] == 0x00 && data[10] == 0x00)
                {
                    Console.WriteLine($"Successfully wrote {value:X4} to W4000");
                }
                else
                {
                    ushort errorCode = (ushort)((data[10] << 8) | data[9]);
                    Console.WriteLine($"Write failed - PLC error: {errorCode:X4}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing W4000: {ex.Message}");
            }
        }
    }
}
