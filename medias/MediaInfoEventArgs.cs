// Version: 1.0.0.665
using MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.medias
{
    public class MediaInfoEventArgs
    {
        public MediaInfo.MediaInfo MediaInfo
        {
            get
            {
                var p = new DirectoryInfo(System.IO.Path.Combine(IntPtr.Size == 4 ? "x86" : "x64", "MediaInfo.dll"));
                var mediaInfo_dll = Path.GetDirectoryName(p.FullName);

                return new MediaInfo.MediaInfo(mediaInfo_dll);
            }
        }

        public MediaInfoEventArgs()
        {
            
        }
    }
}
