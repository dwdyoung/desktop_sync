using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopSync
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private SendThread sendThread = new SendThread();

        private ReceiveThread receiveThread = new ReceiveThread();

        public MainWindow()
        {
            InitializeComponent();
        }


        /** 
         * 发送广播
         */
        private void SendBroadcast(object sender, RoutedEventArgs e)
        {
            sendThread.Start();
            sendBt.IsEnabled = false;
            receiveBt.IsEnabled = false;

            this.WindowState = System.Windows.WindowState.Minimized;
        }


        /** 
         * 停止广播
         */
        private void StopBroadcast(object sender, RoutedEventArgs e)
        {
            sendThread.Stop();
            sendBt.IsEnabled = true;
            receiveBt.IsEnabled = true;

            RecoverThread.Instance().Stop();
            receiveThread.Stop();

            this.WindowState = System.Windows.WindowState.Normal;
            this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;

        }


        /** 
         * 接受广播
         */
        private void ReceiveBroadcast(object sender, RoutedEventArgs e)
        {
            RecoverThread.Instance().SetImageDrawCallback(DrawImage);
            RecoverThread.Instance().Start();
            receiveThread.Start();
            sendBt.IsEnabled = false;
            receiveBt.IsEnabled = false;


            this.WindowStyle = System.Windows.WindowStyle.None;
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        /**
         * 画图的委托方法
         * */
        public void DrawImage(IImage image) {
            BitmapSource bitmapSource = ImageUtils.LoadImage(image.getMemoryStream().ToArray());
            //BitmapImage bitmapImage = new BitmapImage();
            //bitmapImage.BeginInit();
            //bitmapImage.StreamSource = image.getMemoryStream();
            //bitmapImage.EndInit();
            Action<System.Windows.Controls.Image, BitmapSource> updateAction = new Action<System.Windows.Controls.Image, BitmapSource>(showImage);
            deskImage.Dispatcher.BeginInvoke(updateAction, deskImage, bitmapSource);

            
        }


        private void showImage(System.Windows.Controls.Image image, BitmapSource bitmapSource)
        {
            base.Dispatcher.Invoke(delegate
            {
                this.deskImage.Source = bitmapSource;
            });
        }


        /**
         * 测试
         * */
        private void Test(object sender, RoutedEventArgs e)
        {

            // 截图
            ScreenCapture screenCapture = new ScreenCapture();
            
            Bitmap screen = screenCapture.CaptureScreen();

            // Bitmap screen = new Bitmap(@"C:\工具&资源\身份证背面.jpg");
            // screen.Save(@"C:\工具&资源\test.jpg");

            MemoryStream ms = new MemoryStream();
            // screen.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            screen.Save(ms, ImageCodecInfo.GetImageEncoders()[1], null);
            deskImage.Source = ImageUtils.LoadImage(ms.ToArray());


            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < info.Length; i++ )
            {
                Console.WriteLine(info[i].CodecName);
            }


            // screen.Save(ms, ImageCodecInfo.GetImageEncoders()[0], null);

            //JpegBitmapDecoder jpegBitmapDecoder = new JpegBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            //BitmapSource bitmapSource = jpegBitmapDecoder.Frames[0];
            //bitmapSource.Freeze();
            //deskImage.Source = bitmapSource;

            //BitmapImage bitmapImage = new BitmapImage();
            //bitmapImage.BeginInit();
            //bitmapImage.StreamSource = ms;
            //bitmapImage.EndInit();
            //deskImage.Source = bitmapImage;

            //BitmapImage bitmapImage = new BitmapImage(new Uri(@"C:\工具&资源\身份证背面.jpg"));
            //deskImage.Source = bitmapImage;


            // Action<System.Windows.Controls.Image, BitmapImage> updateAction = new Action<System.Windows.Controls.Image, BitmapImage>(showImage);
            // deskImage.Dispatcher.BeginInvoke(updateAction, deskImage, bitmapImage);
        }
    }
}
