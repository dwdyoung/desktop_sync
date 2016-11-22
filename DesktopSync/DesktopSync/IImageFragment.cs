using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopSync
{
    public interface IImageFragment
    {
        // 碎片类型
        byte getType();

        // 碎片实体
        byte[] getBody();

        // 图片大小
        long getSize();

        // 图片编号
        long getSeq();

        // 开始位置
        int getStartIndex();

        // 碎片自编号
        short getChildSeq();
    }
}
