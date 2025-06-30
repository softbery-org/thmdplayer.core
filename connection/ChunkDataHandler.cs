// Version: 1.0.0.546
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.connection
{
    // Przyk�ad u�ycia z obs�ug� du�ych danych
    public class ChunkedDataHandler
    {
        public static byte[] SerializeLargeData<T>(T data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            return Encoding.UTF8.GetBytes(json);
        }

        public static T DeserializeLargeData<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json);
        }

        public static void SendLargeData(NetworkStream stream, byte[] data)
        {
            // Wys�anie rozmiaru danych
            var lengthBytes = BitConverter.GetBytes(data.Length);
            stream.Write(lengthBytes, 0, 4);

            // Wys�anie danych w chunkach
            int offset = 0;
            while (offset < data.Length)
            {
                int chunkSize = Math.Min(4096, data.Length - offset);
                stream.Write(data, offset, chunkSize);
                offset += chunkSize;
            }
        }

        public static byte[] ReceiveLargeData(NetworkStream stream)
        {
            // Odczyt rozmiaru danych
            var lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, 4);
            int length = BitConverter.ToInt32(lengthBytes, 0);

            // Odczyt danych
            var buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunkSize = stream.Read(buffer, offset, length - offset);
                offset += chunkSize;
            }
            return buffer;
        }
    }
}
