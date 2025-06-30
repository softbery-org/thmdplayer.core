// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.Interfaces
{
    public interface IMediaStream : IDisposable
    {
        Task<Stream> GetStreamAsync();
        Task<Stream> GetStream();
        double GetDuration();
    }
}
