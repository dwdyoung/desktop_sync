using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopSync
{

    /**
     * 打包线程，用于将接收的包打包成图片，发布到显示
     * 
     * */
    public class RecoverThread
    {
        private Thread thread = null;

        private Boolean running = false;

        private static Object mu = new Object();

        private ConcurrentQueue<IImageFragment> imageFrameList;

        private MemoryStream imageMemory;

        private DrawImage myDrawDelegate;

        private short lastChildSeq;


        void Run()
        {
            while (running)
            {
                IImageFragment fragment;
                if (imageFrameList.TryDequeue(out fragment))
                {
                    if(!running){
                        return;
                    }
                    // 开始码
                    if(fragment.getType() == 0x01){
                        if(imageMemory != null){
                            imageMemory.Close();
                        }
                        imageMemory = new MemoryStream();
                        imageMemory.Write(fragment.getBody(), fragment.getStartIndex() + 25, fragment.getBody().Length - 25);
                        lastChildSeq = fragment.getChildSeq();
                        continue;
                    }

                    // 结束码
                    if(fragment.getType() == 0x02){

                        // 检查是否有值
                        if (imageMemory.Length == 0)
                        {
                            continue;
                        }

                        // 检查图片编号一致性
                        // 如果顺序不一致，则写上空白数据
                        int offset = fragment.getChildSeq() - lastChildSeq;
                        if (offset > 1)
                        {
                            offset--;
                            int empty = offset * 999;
                            imageMemory.Write(new byte[empty], 0, empty);
                        }

                        imageMemory.Write(fragment.getBody(), fragment.getStartIndex() + 25, fragment.getBody().Length - 25);
                        lastChildSeq = 0;
                        // TODO 检查图片完整性 
                        if (imageMemory.Length < fragment.getSize())
                        {
                            // 如果字节不够，写入空白buffer
                            long size = fragment.getSize() - imageMemory.Length;
                            byte[] empty = new byte[size];
                            imageMemory.Write(empty, 0, empty.Length);
                            Console.WriteLine("图片大小不完整，原图大小为{0}, 实际大小为 {1}", fragment.getSize(), imageMemory.Length);
                            // continue;
                        }

                        // 组成图片写入到界面
                        if (myDrawDelegate != null)
                        {
                            // Bitmap bitmap = new Bitmap(imageMemory);
                            IImage image = new UdpImage(imageMemory);
                            myDrawDelegate(image);
                        }
                        continue;
                    }

                    // 身体
                    if(fragment.getType() == 0x03){

                        // 检查是否有值
                        if (imageMemory.Length == 0)
                        {
                            continue;
                        }

                        // 检查图片编号一致性
                        int offset = fragment.getChildSeq() - lastChildSeq;
                        if(offset > 1){
                            offset--;
                            int empty = offset * 999;
                            imageMemory.Write(new byte[empty], 0, empty);
                        }

                        // TODO 检查图片完整性 

                        imageMemory.Write(fragment.getBody(), fragment.getStartIndex() + 25, fragment.getBody().Length - 25);
                        lastChildSeq = fragment.getChildSeq();
                        continue;
                    }

                    
                }
            }
        }


        public void SetImageDrawCallback(DrawImage drawImageDelegate) {
            myDrawDelegate = drawImageDelegate;
        }


        public RecoverThread() {
            imageFrameList = new ConcurrentQueue<IImageFragment>();
        }


        // 推送任务
        public void Push(IImageFragment fragment) { 
            imageFrameList.Enqueue(fragment);
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
            imageFrameList.Enqueue(new UdpImageFragment(0, 0, 0, null, 0, 0));

        }

        private static RecoverThread instance;

        public static RecoverThread Instance() { 
            if(instance == null){
                instance = new RecoverThread();
            }
            return instance;
        }

        // 画图的委托
        public delegate void DrawImage(IImage image);

    }
}
