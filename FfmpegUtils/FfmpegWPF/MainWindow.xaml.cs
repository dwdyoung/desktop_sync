using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

namespace FfmpegWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        H264FileThread h264FileThread;

        H264MultiUdpThread h264MultiUdpThread;

        H264SendUdpThread h264SendUdpThread;

        public MainWindow()
        {
            InitializeComponent();
            instance = this;

            sendBt.IsEnabled = true;
            openFileBt.IsEnabled = true;
            receiveBt.IsEnabled = true;
            stopBt.IsEnabled = false;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            sendBt.IsEnabled = false;
            openFileBt.IsEnabled = false;
            receiveBt.IsEnabled = false;
            stopBt.IsEnabled = true;

            string filename = h264filePath.Text;
            h264FileThread = new H264FileThread(pictureRgbCallbackFunction, filename);
            h264FileThread.start();

        }

        public static void pictureRgbCallbackFunction(SimpleFrame simpleFrame)
        {
            Action<System.Windows.Controls.Image, SimpleFrame> updateAction =
                new Action<System.Windows.Controls.Image, SimpleFrame>(showBitmap);

            getInstance().yuvImage.Dispatcher.BeginInvoke(updateAction, getInstance().yuvImage, simpleFrame);

        }

        // 显示图片的代理方法
        public static void showBitmap(System.Windows.Controls.Image image, SimpleFrame simpleFrame)
        {
            getInstance()._showBitmap(image, simpleFrame);
        }

        public void _showBitmap(System.Windows.Controls.Image image, SimpleFrame simpleFrame) 
        {
            base.Dispatcher.Invoke(delegate
            {
                BitmapSource bitmap = FfmpegUtils.RgbByteArrayToBitmapImage(simpleFrame.data, simpleFrame.width, simpleFrame.height);

                yuvImage.BeginInit();
                yuvImage.Source = bitmap;
                yuvImage.EndInit();
            });
        }
       

        private static MainWindow instance;
        public static MainWindow getInstance() {
            return instance;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            sendBt.IsEnabled = false;
            openFileBt.IsEnabled = false;
            receiveBt.IsEnabled = false;
            stopBt.IsEnabled = true;

            string address = addressTx.Text;
            int port = Int32.Parse(portTx.Text);
            h264MultiUdpThread = new H264MultiUdpThread(pictureRgbCallbackFunction, address, port);
            h264MultiUdpThread.start();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            sendBt.IsEnabled = false;
            openFileBt.IsEnabled = false;
            receiveBt.IsEnabled = false;
            stopBt.IsEnabled = true;

            string address = addressTx.Text;
            int port = Int32.Parse(portTx.Text);
            h264SendUdpThread = new H264SendUdpThread(address, port);
            h264SendUdpThread.start();
        }

        private void stopBt_Click(object sender, RoutedEventArgs e)
        {
            sendBt.IsEnabled = true;
            openFileBt.IsEnabled = true;
            receiveBt.IsEnabled = true;
            stopBt.IsEnabled = false;

            if (h264FileThread != null)
            {
                h264FileThread.close();
                h264FileThread = null;
            }

            if (h264MultiUdpThread != null)
            {
                h264MultiUdpThread.close();
                h264MultiUdpThread = null;
            }

            if (h264SendUdpThread != null)
            {
                h264SendUdpThread.close();
                h264SendUdpThread = null;
            }
        }
    }
}
