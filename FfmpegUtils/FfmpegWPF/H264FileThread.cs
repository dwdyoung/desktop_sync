using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FfmpegWPF
{
    // 读取H264文件，解释成视频的线程
    class H264FileThread
    {
        Thread t;

        static FfmpegUtils.SimpleFrameDecodeCallback callback;

        static FfmpegUtils.DecodeCallback decodeCallback;

        string filename;

        bool running = true;

        public H264FileThread(FfmpegUtils.SimpleFrameDecodeCallback callback, string filename)
        {
            H264FileThread.callback = callback;
            this.filename = filename;
            unsafe {
                H264FileThread.decodeCallback = new FfmpegUtils.DecodeCallback(H264FileThread.pictureRgbCallbackFunction);
            }
        }

        public void start() 
        {
            if(t != null){
                return;
            }
            t = new Thread(run);
            t.Start();
        }


        static unsafe void pictureRgbCallbackFunction(byte** data, int* linesize, int linecsizeLength, int width, int height)
        {
            byte[] frameData = FfmpegUtils.RgbBytePointerToByteArray(data, width, height);

            SimpleFrame simpleFrame = new SimpleFrame();
            simpleFrame.data = frameData;
            simpleFrame.width = width;
            simpleFrame.height = height;

            callback(simpleFrame);
        }


        void run() 
        {
            unsafe
            {
                int res = FfmpegUtils.initDecoder();
                res = FfmpegUtils.setDecoderRgbCallback(decodeCallback);
            }


            // 讀取h264文件, 獲取幀數據
            FileStream readStream = new FileStream(filename, FileMode.Open);
            byte[] data = new byte[10240];
            byte[] frameData = new byte[102400];
            int frameLength = 0;
            int frameCount = 0;
            while (running)
            {
                int length = readStream.Read(data, 0, data.Length);
                if (length == 0)
                {
                    // 最后一帧
                    byte[] findFrame = new byte[frameLength];
                    Array.Copy(frameData, 0, findFrame, 0, frameLength);
                    int res = FfmpegUtils.pushH264(findFrame, frameLength);

                    Console.WriteLine("读取结束");
                    break;
                }

                Array.Copy(data, 0, frameData, frameLength, length);
                frameLength += length;

                int head = FfmpegUtils.findHead(frameData, 4, frameLength - 4);
                while (head > 0)
                {
                    frameCount += 1;
                    byte[] findFrame = new byte[head];
                    Array.Copy(frameData, 0, findFrame, 0, head);
                    frameLength -= head;
                    int res = FfmpegUtils.pushH264(findFrame, head);
                    Thread.Sleep(40);

                    byte[] temp = new byte[102400];
                    Array.Copy(frameData, head, temp, 0, frameLength);
                    frameData = temp;

                    head = FfmpegUtils.findHead(frameData, 4, frameLength - 4);
                }

            }
            readStream.Close();
        }

        // 关闭线程
        public void close()
        {
            running = false;
        }
    }
}
