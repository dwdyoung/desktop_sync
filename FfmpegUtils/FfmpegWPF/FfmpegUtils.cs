using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FfmpegWPF
{
    class FfmpegUtils
    {
        public const byte FRAME_HEAD = 0x01;

        public const byte FRAME_FOOT = 0X02;

        public const byte FRAME_BODY = 0X03;

        public const byte HOLD_FRAME = 0X07;


        // 解码之后的回调
        public unsafe delegate void DecodeCallback(byte** data, int* linesize, int linecsizeLength, 
            int width, int height);

        public unsafe delegate void EncoderCallback(IntPtr data, int length);

        public delegate void SimpleFrameDecodeCallback(SimpleFrame simpleFrame);

        static DecodeCallback callback;// 回调实例

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi, 
            CallingConvention = CallingConvention.StdCall)]
        public static extern int test();

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int initDecoder();

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int initEncoder();

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern int pushH264(byte* h264Data, int length);

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern int setDecoderRgbCallback(DecodeCallback callback);

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern int setEncoderCallback(EncoderCallback callback);

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int tryToMakeFrame();

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
           CallingConvention = CallingConvention.Cdecl)]
        public static extern int closeDecoder();

        [DllImport(@"FfmpegUtils.dll", CharSet = CharSet.Ansi,
           CallingConvention = CallingConvention.Cdecl)]
        public static extern int closeEncoder();


        // 推送h264数据
        public static int pushH264(byte[] h264Data, int length) 
        {
            unsafe
            {
                fixed (byte* pFindFrame = h264Data)
                {
                    return pushH264(pFindFrame, length);
                }
            }
        }




        // rgb数据转图像
        public static BitmapSource RgbByteArrayToBitmapImage(byte[] byteArray, int width, int height)
        {

            //WriteableBitmap bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            //bmp.WritePixels(
            //    new Int32Rect(0, 0, simpleFrame.width, simpleFrame.height), 
            //    simpleFrame.data, 
            //    writeBitmap.BackBufferStride, 
            //    0);
            //return bmp


            BitmapSource bmp = null;
            bmp = BitmapImage.Create(width, height, 96, 96,
                PixelFormats.Rgb24, null, byteArray, (width * PixelFormats.Rgb24.BitsPerPixel + 7) / 8);

            return bmp;
        }


        // rgb数据指针转数组
        public unsafe static byte[] RgbBytePointerToByteArray(byte** data, int width, int height) 
        {
            int dataLength = width * height * 3;
            byte[] frameData = new byte[dataLength];
            Marshal.Copy((IntPtr)data[0], frameData, 0, dataLength);
            return frameData;
        }



        /// <summary>  
        /// 获取时间戳  
        /// </summary>  
        /// <returns></returns>  
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }


        public unsafe static int findHeadUnsafe(byte* buffer, int offset, int len)
        {
            int i;
            if (len == 0)
                return -1;
            for (i = offset; i < len; i++)
            {
                if (checkHeadUnsafe(buffer, i))
                    return i;
            }
            return -1;
        }

        unsafe static bool checkHeadUnsafe(byte* buffer, int offset)
        {
            // 00 00 00 01
            if (buffer[offset] == 0x00 && buffer[offset + 1] == 0x00
                    && buffer[offset + 2] == 0x00 && buffer[3] == 0x01)
                return true;
            // 00 00 01
            if (buffer[offset] == 0x00 && buffer[offset + 1] == 0x00
                    && buffer[offset + 2] == 0x01)
                return true;
            return false;
        }


        public static int findHead(byte[] buffer, int offset, int len)
        {
            int i;
            if (len == 0)
                return -1;
            for (i = offset; i < len; i++)
            {
                if (checkHead(buffer, i))
                    return i;
            }
            return -1;
        }


        static bool checkHead(byte[] buffer, int offset)
        {
            // 00 00 00 01
            if (buffer[offset] == 0x00 && buffer[offset + 1] == 0x00
                    && buffer[offset + 2] == 0x00 && buffer[3] == 0x01)
                return true;
            // 00 00 01
            if (buffer[offset] == 0x00 && buffer[offset + 1] == 0x00
                    && buffer[offset + 2] == 0x01)
                return true;
            return false;
        }




    }
}
