using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FfmpegWPF
{
    // 监听组播，解释成视频的线程
    class H264MultiUdpThread
    {
        Thread h264Thread;

        Thread receiveThread;

        Socket receiveSocket;

        EndPoint ep;

        static FfmpegUtils.SimpleFrameDecodeCallback callback;

        static FfmpegUtils.DecodeCallback decodeCallback;

        bool running = true;

        string address;

        int port;

        int total = 1;

        Dictionary<long, FrameInfo> frameInfoMap = new Dictionary<long,FrameInfo>();

        SizeQueue<FrameInfo> frameQueue = new SizeQueue<FrameInfo>(100);

        private LinkedList<long> seqLink = new LinkedList<long>();

        public H264MultiUdpThread(FfmpegUtils.SimpleFrameDecodeCallback callback, string address, int port)
        {
            H264MultiUdpThread.callback = callback;
            this.address = address;
            this.port = port;
            unsafe
            {
                H264MultiUdpThread.decodeCallback = new FfmpegUtils.DecodeCallback(H264MultiUdpThread.pictureRgbCallbackFunction);
            }
        }


        public void start() {
            if (h264Thread != null)
            {
                return;
            }
            h264Thread = new Thread(h264Run);
            h264Thread.Start();

            receiveThread = new Thread(receiveRun);
            receiveThread.Start();
        }


        // h264转化线程
        void h264Run()
        {

            FfmpegUtils.initDecoder();
            unsafe 
            {
                FfmpegUtils.setDecoderRgbCallback(decodeCallback);
            }
            while (running)
            {
                // 将数据推送到ffmpeg
                FrameInfo frameInfo = frameQueue.Dequeue();

                if (frameInfo.frameData != null)
                {
                    FfmpegUtils.pushH264(frameInfo.frameData, frameInfo.frameData.Length);
                }
            }

            FfmpegUtils.closeDecoder();
        }


        // 转化成功回调
        static unsafe void pictureRgbCallbackFunction(byte** data, int* linesize, int linecsizeLength, int width, int height)
        {
            byte[] frameData = FfmpegUtils.RgbBytePointerToByteArray(data, width, height);

            SimpleFrame simpleFrame = new SimpleFrame();
            simpleFrame.data = frameData;
            simpleFrame.width = width;
            simpleFrame.height = height;

            callback(simpleFrame);
        }


        // 初始化接收端口
        void initSocket() {
            receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, port);
            ep = (EndPoint)iep;
            receiveSocket.Bind(iep);
            receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                new MulticastOption(IPAddress.Parse(address)));
        }

        // 线程执行的run方法
        void receiveRun()
        {
            initSocket();

            while (running)
            {
                byte[] buffer = new byte[1024];
                int receiverLength = 0;
                try
                {
                    receiverLength = receiveSocket.ReceiveFrom(buffer, ref ep);
                }
                catch
                {
                    continue;
                }
                

                // TODO 接收到数据
                // 图片编号
                long seq = (buffer[4] & 0xFF) |
                        ((buffer[5] << 8) & 0xFF00) |
                        ((buffer[6] << 16) & 0xFF0000) |
                        ((buffer[7] << 24) & 0xFF000000) |
                        ((buffer[8] << 32) & 0xFF00000000L) |
                        ((buffer[9] << 40) & 0xFF0000000000L) |
                        ((buffer[10] << 48) & 0xFF000000000000L) |
                        ((long)(((ulong)buffer[11] << 56) & 0xFF00000000000000L));

                // 图片子编号
                short childSeq =
                        (short)((buffer[12] & 0xFF) |
                                ((buffer[13] <<8) & 0xFF00));

                // 包类型
                byte type = buffer[14];

                // 图片大小
                long size =
                        (buffer[15] & 0xFF) |
                                ((buffer[16] << 8) & 0xFF00) |
                                ((buffer[17] << 16) & 0xFF0000) |
                                ((buffer[18] << 24) & 0xFF000000) |
                                ((buffer[19] << 32) & 0xFF00000000L) |
                                ((buffer[20] << 40) & 0xFF0000000000L) |
                                ((buffer[21] << 48) & 0xFF000000000000L) |
                                ((long)(((ulong)buffer[22] << 56) & 0xFF00000000000000L));

                // 包长度
                short length =
                        (short)((buffer[23] & 0xFF) |
                                ((buffer[24] <<8) & 0xFF00));

                total ++;

                long packSeq = seq * 10000 + childSeq;

                if(seqLink.Contains(packSeq)){
                    // 包重复
                    continue;
                }


                if (pushPiece(buffer, receiverLength, type, seq, childSeq, size))
                {
                    seqLink.AddLast(packSeq);

                    if(seqLink.Count() > 10){
                        seqLink.RemoveFirst();
                    }
                } else {

                }
            }
        }


        // 将h264数据推送到另一个线程
        public bool pushPiece(byte[] data, int length,
                             byte type, long seq, short childSeq, long frameLength)
        {
            if(type == FfmpegUtils.HOLD_FRAME){
                // 已经是一个完整的帧, 可以直接推送
                FrameInfo frameInfo = new FrameInfo();
                frameInfo.complete = true;
                byte[] frameData = new byte[length - 25];
                Array.Copy(data, 25, frameData, 0, frameData.Length);
                frameInfo.frameData = frameData;

                // 推送到显示
                frameQueue.Enqueue(frameInfo);


                // 检查是否有超时的帧,删除掉
                List<long> removeList = new List<long>();
                foreach (KeyValuePair<long, FrameInfo> pair in frameInfoMap)
                {
                    long now = FfmpegUtils.GetTimeStamp();
                    if(now - pair.Value.createTime > 1000){
                        removeList.Add(pair.Key);
                    }
                }

                foreach(long remove in removeList){
                    frameInfoMap.Remove(remove);
                }
                return true;

            } else if(type == FfmpegUtils.FRAME_HEAD){

                // 收到I帧头, 创建碎片空间
                FrameInfo frameInfo = new FrameInfo();
                byte[] frameData = new byte[(int)frameLength];
                Array.Copy(data, 25, frameData, 0, length - 25);
                frameInfo.frameData = frameData;
                frameInfoMap.Add(seq, frameInfo);
                return true;
            } else if(type == FfmpegUtils.FRAME_BODY){
                if (!frameInfoMap.ContainsKey(seq))
                {
                    return false;
                }
                FrameInfo frameInfo = frameInfoMap[seq];
                if(frameInfo == null){
                    return false;
                }

                byte[] frameData = frameInfo.frameData;
                Array.Copy(data, 25, frameData, childSeq * 999, length - 25);

                // 如果超过100毫秒就输出
                if(FfmpegUtils.GetTimeStamp() - frameInfo.createTime > 100){
                    frameQueue.Enqueue(frameInfo);
                    frameInfoMap.Remove(seq);
                    return true;
                }
                return true;
            } else if(type == FfmpegUtils.FRAME_FOOT){
                if (!frameInfoMap.ContainsKey(seq))
                {
                    return false;
                }
                FrameInfo frameInfo = frameInfoMap[seq];
                if(frameInfo == null){
                    return false;
                }

                byte[] frameData = frameInfo.frameData;
                Array.Copy(data, 25, frameData, childSeq * 999, length - 25);
                // 推送到显示

                frameQueue.Enqueue(frameInfo);
                frameInfoMap.Remove(seq);

                return true;

            }

            return false;
        }


        // 关闭线程
        public void close() {
            running = false;
            frameQueue.Enqueue(new FrameInfo());
            receiveSocket.Close();
        }
    }
}
