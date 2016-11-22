using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
     * 广播线程，包含截图，打包并广播
     * 
     * */
    class SendThread 
    {
        private const int SIZE_PER_PACKET = 1024;

        private const byte TYPE_START = 0X01;

        private const byte TYPE_END = 0X02;

        private const byte TYPE_BODY = 0X03;

        private Thread thread;

        private Boolean running = false;

        // private UdpUtils udpUtils = UdpUtils.Instance();

        private long packageSeq = 0;

        private UdpClient sender = new UdpClient();         // 

        private IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, Config.UDP_PORT);

        private IPEndPoint groupEP2 = new IPEndPoint(IPAddress.Broadcast, 11001);

        public SendThread() {
        }

        void Run()
        {
            while (running) {

                Stopwatch st = new Stopwatch();
                st.Reset();
                st.Start();

                ScreenCapture screenCapture = new ScreenCapture();
                Bitmap screen = screenCapture.CaptureScreen();
                MemoryStream ms = new MemoryStream();
                // screen.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ImageUtils.SaveToStream(screen, ms, Config.JPEG_QTY);

                Console.WriteLine("image size is : {0}", ms.Length);

                ms.Position = 0;

                packageSeq++;

                // 图片总大小
                byte[] imageLength = BitConverter.GetBytes(ms.Length);
                IntPtr pImageLength = Marshal.AllocHGlobal(imageLength.Length);
                Marshal.Copy(imageLength, 0, pImageLength, imageLength.Length);

                short childSeq = 0;
                while (ms.Position < ms.Length){

                    const int readSize = SIZE_PER_PACKET - 25;
                    byte[] data ;
                    if (ms.Position + readSize > ms.Length)
                    {
                        data = new byte[ms.Length - ms.Position + 25];
                    } 
                    else 
                    {
                        data = new byte[SIZE_PER_PACKET];   
                    }
                    // 头占4byte 0x00 0x00 0x00 0x01
                    data[3] = 0x01;

                    // 图片编号 占8byte long型
                    byte[] seqData = BitConverter.GetBytes(packageSeq);
                    IntPtr pSeq = Marshal.AllocHGlobal(seqData.Length);
                    Marshal.Copy(seqData, 0, pSeq, seqData.Length);
                    Marshal.Copy(pSeq, data, 4, seqData.Length);

                    // 图片子编号
                    childSeq++;
                    byte[] childSeqData = BitConverter.GetBytes(childSeq);
                    // IntPtr pChildSeq = Marshal.AllocHGlobal(childSeqData.Length);
                    // Marshal.Copy(childSeqData, 0, pChildSeq, seqData.Length);
                    // Marshal.Copy(pChildSeq, data, 12, seqData.Length);
                    data[12] = childSeqData[0];
                    data[13] = childSeqData[1];

                    // 留一位保存包类型


                    // 图片总大小
                    Marshal.Copy(pImageLength, data, 15, 8);

                    int readed = ms.Read(data, 25, data.Length - 25);
                    short size = (short)(readed + 25);

                    // 包长度
                    byte[] sizeData = BitConverter.GetBytes(size);
                    // IntPtr pSize = Marshal.AllocHGlobal(sizeData.Length);
                    // Marshal.Copy(sizeData, 0, pSize, sizeData.Length);
                    // Marshal.Copy(pSize, data, 14, sizeData.Length);
                    data[23] = sizeData[0];
                    data[24] = sizeData[1];

                    // 包类型
                    if (ms.Position == SIZE_PER_PACKET - 25)
                    {
                        data[14] = TYPE_START;
                    }
                    else if (ms.Position >= ms.Length)
                    {
                        data[14] = TYPE_END;
                    }
                    else 
                    {
                        data[14] = TYPE_BODY;
                    }


                    // 广播11000
                    try
                    {
                        sender.Send(data, data.Length, groupEP);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    //// 广播11000
                    //try
                    //{
                    //    sender.Send(data, data.Length, groupEP2);
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine(e.ToString());
                    //}
                } 

                st.Stop();
                Console.WriteLine("截图时间： {0}", st.ElapsedMilliseconds);
            }
        }

        public void Start() 
        { 
            if(thread != null ){
                thread.Abort();
                thread = null;
            }
            running = true;
            thread = new Thread(Run);
            thread.Start();
        }

        public void Stop() 
        {
            running = false;
            if(thread != null){
                thread.Abort();
                thread = null;
            }
        }
    }

}
