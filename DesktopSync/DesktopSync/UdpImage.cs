using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSync
{
    public class UdpImage : IImage
    {

        private MemoryStream bitmap;

        public UdpImage(MemoryStream bitmap) 
        {
            this.bitmap = bitmap;
        }

        public MemoryStream getMemoryStream()
        {
            return bitmap;
            throw new NotImplementedException();
        }
    }
}
