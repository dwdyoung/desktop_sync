using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopSync
{
    /**
     * 接收线程，用于监听端口，做简单的转化之后将byte交给RecoverThread处理
     * 
     * */
    class ReceiveThread 
    {
        private Thread thread = null;

        private Boolean running = false;

        // private UdpUtils udpUtils = UdpUtils.Instance();

        private static readonly IPAddress GroupAddress = IPAddress.Parse("127.0.0.1");        // 广播地址

        private UdpClient listener;

        void Run()
        {
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, Config.UDP_PORT);

            listener = new UdpClient(groupEP);

            try
            {
                while (running)
                {
                    byte[] bytes = listener.Receive(ref groupEP);
                    if(bytes.Length < 26){
                        continue;
                    }
                    if(!running){
                        return;
                    }

                    // 将零散的byte拼成图片
                    int startIndex = findStart(bytes);                  // 开始码的位置
                    long seq = BitConverter.ToInt64(bytes, 4);          // 图片编号
                    short childSeq = BitConverter.ToInt16(bytes, 12);   // 子编号
                    byte type = bytes[14];                              // 包类型
                    long imageSize = BitConverter.ToInt64(bytes, 15);   // 图片大小
                    short size = BitConverter.ToInt16(bytes, startIndex + 23);  // 包长度

                    // 判断包是否完整，不完整则丢弃 TODO 暂时不处理丢包问题
                    if (size < bytes.Length)
                    {
                        Console.WriteLine("package not enough");
                        continue;
                    }

                    // byte[] data = new byte[size];
                    // IntPtr pData = Marshal.AllocHGlobal(data.Length);
                    // Marshal.Copy(bytes, startIndex, pData, data.Length);
                    // Marshal.Copy(pData, data, 12, data.Length);

                    UdpImageFragment fragment = new UdpImageFragment(type, imageSize, seq, bytes, startIndex, childSeq);
                    RecoverThread.Instance().Push(fragment);
                    
                }

                listener.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Start() 
        { 
            running = true;
            thread = new Thread(Run);
            thread.Start();
        }

        public void Stop() 
        {
            running = false;
            try
            {
                listener.Close();
            }
            catch 
            { 
                
            }
            listener = null;
        }


        /**
         * 寻找开始码
         * */
        private int findStart(byte[] data) {
            int index = 0;
            while(index + 3 < data.Length){
                if(data[index] == 0x00 && data[index + 1] == 0x00 && data[index + 2] == 0x00 && data[index + 3] == 0x01){
                    return index;
                }
            }
            return -1;
        }
    }

}
