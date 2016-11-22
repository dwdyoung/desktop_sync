using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FfmpegWPF
{
    public class FrameInfo
    {
        public FrameInfo(byte[] frameData, long presentationTime)
        {
            this.frameData = frameData;
            this.presentationTime = presentationTime;
            this.createTime = FfmpegUtils.GetTimeStamp();
        }

        public FrameInfo()
        {
            this.createTime = FfmpegUtils.GetTimeStamp();
        }

        public long createTime {get; set;}

        public bool complete { get; set; }

        public byte[] frameData { get; set; }

        public long presentationTime { get; set; }
    }
}
