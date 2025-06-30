// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.Interfaces;

namespace ThmdPlayer.Core.medias
{
    public class NetworkMediaStream : IMediaStream
    {
        private readonly string _url;

        public NetworkMediaStream(string url)
        {
            _url = url;
        }

        public async Task<Stream> GetStreamAsync()
        {
            using (var client = new HttpClient())
            {
                return await client.GetStreamAsync(_url);
            }
        }

        public double GetDuration()
        {
            // Tutaj można dodać logikę do odczytu metadanych strumienia sieciowego i uzyskania czasu trwania
            return 180; // Domyślna wartość
        }

        public Task<Stream> GetStream()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

}
