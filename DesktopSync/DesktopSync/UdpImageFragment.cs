using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSync
{
    class UdpImageFragment : IImageFragment
    {
        private byte type;

        private byte[] body;

        private long size;

        private long seq;

        private int startIndex;

        private short childSeq;

        public UdpImageFragment(byte type, long size, long seq, byte[] body, int startIndex, short childSeq)
        {
            this.type = type;
            this.size = size;
            this.seq = seq;
            this.body = body;
            this.startIndex = startIndex;
            this.childSeq = childSeq;
        }

        public byte getType()
        {
            return type;
            throw new NotImplementedException();
        }

        public byte[] getBody()
        {
            return body;
            throw new NotImplementedException();
        }

        public long getSize()
        {
            return size;
            throw new NotImplementedException();
        }

        public long getSeq()
        {
            return seq;
            throw new NotImplementedException();
        }


        public int getStartIndex()
        {
            return startIndex;
            throw new NotImplementedException();
        }

        public short getChildSeq()
        {
            return childSeq;
        }
    }
}
