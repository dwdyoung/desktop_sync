using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DesktopSync
{
    public interface IImage
    {
        MemoryStream getMemoryStream();
    }
}
