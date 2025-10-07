using System;
using System.Net.Sockets;

namespace SLMP_SampleFrame.Services
{
    public class PlcWriter
    {
        private readonly PlcConnection _connection;

        public PlcWriter(PlcConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        public void WriteW04001(int value)
        {
            // Request frame for write W04001 register
            byte[] payload = new byte[] {
                0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00,
                0x0E, 0x00, 0x14, 0x00, 0x01, 0x14, 0x00,
                0x00, 0x01, 0x04, 0x00, 0xB4, 0x01, 0x00,
                (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF)
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
                    Console.WriteLine("Write W04001 finished correctly!");
                    Console.WriteLine($"Written value W04001 (DEC): {value}");
                    Console.WriteLine($"Written value W04001 (HEX): {value:X4}");
                }
                else
                {
                    Console.WriteLine("Error in response");
                    throw new InvalidOperationException("PLC returned error response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during write: {ex.Message}");
                throw;
            }
        }
    }
}