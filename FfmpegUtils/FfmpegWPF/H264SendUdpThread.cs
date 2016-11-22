using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FfmpegWPF
{
    class H264SendUdpThread
    {
        Thread t;

        static FfmpegUtils.EncoderCallback callback;

        bool running = true;

        static long seq = 0;

        static Socket socket;

        static IPEndPoint multicast;

        static bool firstFrame = true;

        static byte[] spsPpsData;

        public H264SendUdpThread(string address, int port)
        {
            unsafe {
                H264SendUdpThread.callback = new FfmpegUtils.EncoderCallback(H264SendUdpThread.onH264FrameCreate);
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            multicast = new IPEndPoint(IPAddress.Parse(address), port); 
        }

        public void start() 
        {
            if(t != null){
                return;
            }
            t = new Thread(run);
            t.Start();
        }

        // 生成的h264回调
        static void onH264FrameCreate(IntPtr data, int length)
        {
            bool keyFrame = false;
            unsafe {

                // 判断是否是关键帧
                byte* point = (byte*)data.ToPointer();
                byte info = point[4];
                byte headInfo = (byte)(info & 0x1F);
                keyFrame = headInfo == 7;
            }

            byte[] h264Data = GetBytes(data, length, keyFrame);

            if(keyFrame){
                sendData(spsPpsData, 0);
            }

            sendData(h264Data, 0);
        }


        void run() 
        {
            unsafe
            {
                int res = FfmpegUtils.initEncoder();
                res = FfmpegUtils.setEncoderCallback(callback);
            }
            
            while (running)
            {
                FfmpegUtils.tryToMakeFrame();
                Thread.Sleep(10);
            }

            FfmpegUtils.closeEncoder();
            socket.Close();
            socket = null;
        }

        private unsafe static byte[] GetBytes(IntPtr buffer, int size, bool keyFrame)
        {
            if (keyFrame)
            {
                byte* h264Data = (byte*)buffer.ToPointer();

                // 提取sps pps
                int spsHead = FfmpegUtils.findHeadUnsafe(h264Data, 4, size - 4);
                int ppsHead = FfmpegUtils.findHeadUnsafe(h264Data, spsHead + 4, size - 4 - spsHead);

                if (firstFrame)
                {
                    firstFrame = false;
                    spsPpsData = new byte[ppsHead];
                    Marshal.Copy(buffer, spsPpsData, 0, ppsHead);
                }

                // 提取i帧，把前面的spsPps去除掉
                byte[] bytes = new byte[size - ppsHead];
                Marshal.Copy(buffer + ppsHead, bytes, 0, size - ppsHead);
            }


            byte[] bytes2 = new byte[size];
            Marshal.Copy(buffer, bytes2, 0, size);
            return bytes2;
        }


        // 发送数据
        static void sendData(byte[] data, long presentationTimeUs) {

            // 写入文件
            //FileStream fileStream = new FileStream("./out.h264", FileMode.Append);
            //fileStream.Write(data, 0, data.Length);
            //fileStream.Close();

            seq++;
            short childSeq = -1;

            MemoryStream buffer = new MemoryStream(data);
            buffer.Position = 0;
            buffer.SetLength(data.Length);

            const int readSize = 1024 - 25;
            bool totalFrame = false;             // 刚好一个包可以容下一个frame
            while (buffer.Position < buffer.Length)
            {
                childSeq++;
                byte[] spitData;
                if (readSize >= buffer.Length)
                {
                    totalFrame = true;
                }
                if (buffer.Position + readSize > buffer.Length)
                {
                    spitData = new byte[buffer.Length - buffer.Position + 25];
                }
                else
                {
                    spitData = new byte[1024];
                }

                spitData[3] = 0x01;
                putLong(spitData, seq, 4);       // 图片编号
                putShort(spitData, childSeq, 12);    // 图片自编号

                buffer.Read(spitData, 25, spitData.Length - 25);

                if (totalFrame)
                {
                    putByte(spitData, FfmpegUtils.HOLD_FRAME, 14);
                }
                else
                {
                    if (buffer.Position == readSize)
                    {
                        putByte(spitData, FfmpegUtils.FRAME_HEAD, 14);
                    }
                    else if (buffer.Position == buffer.Length)
                    {
                        putByte(spitData, FfmpegUtils.FRAME_FOOT, 14);
                    }
                    else
                    {
                        putByte(spitData, FfmpegUtils.FRAME_BODY, 14);
                    }
                }

                putLong(spitData, data.Length, 15);
                putShort(spitData, (short)spitData.Length, 23);

                socket.SendTo(spitData, multicast);
            }
        }



        /**
         * long 转byte
         * @param bb
         * @param x
         * @param index
         */
        public static void putLong(byte[] bb, long x, int index)
        {
            for (int i = 0; i < 8; i++)
            {
                bb[index + i] = (byte)x;
                x = x >> 8;
            }
        }


        /**
	      * 转换short为byte
	      *
	      * @param b
	      * @param s 需要转换的short
	      * @param index
	      */
        public static void putShort(byte[] b, short s, int index)
        {
	         b[index + 0] = (byte) (s >> 0);
	         b[index + 1] = (byte) (s >> 8);
	    }



        public static void putByte(byte[] bb, byte ch, int index)
        {

            // byte[] b = new byte[2];

            // 将最高位保存在最低位
            bb[index] = ch;
        }


        // 关闭线程
        public void close()
        {
            running = false;
        }
    }
}
