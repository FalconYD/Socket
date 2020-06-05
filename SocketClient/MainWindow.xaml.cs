using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.Windows.Interop;
using System.Threading;
using System.Windows.Threading;

namespace SocketClient
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        bool bSend = false;
        const int nMaxBufferCount = 1024;
        Thread clientThread = null;
        NetworkStream stream = null;
        TcpClient client = null;
        string strIP;
        string strPort;
        public MainWindow()
        {
            InitializeComponent();
            ComponentDispatcher.ThreadIdle += UIInit;
        }

        private void UIInit(object sender, EventArgs args)
        {
            ComponentDispatcher.ThreadIdle -= UIInit;
            strIP = "127.0.0.1";
            strPort = "21000";
            tb_IP.Text = strIP;
            tb_Port.Text = strPort;
        }

        private void SendAsClient()
        {
            try
            {
                if (client != null)
                {
                    return;
                }
                client = new TcpClient();
                client.Connect(IPAddress.Parse(strIP), Convert.ToInt32(strPort));
                
                stream = client.GetStream();

                byte[] message = new byte[nMaxBufferCount];
                string strMsg;
                int byteRead;
               
                while (bSend)
                {
                    if (stream.DataAvailable)
                    {
                        byteRead = stream.Read(message, 0, message.Length);
                        strMsg = Encoding.ASCII.GetString(message);
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            list_Message.Items.Add(DateTime.Now.ToString("[yyyymmdd_hh.mm.ss]") + strMsg);
                        }));
                    }
                }
                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void bn_Run_Click(object sender, RoutedEventArgs e)
        {
            bSend = true;
            clientThread = new Thread(new ThreadStart(SendAsClient));
            clientThread.Start();
        }

        private void bn_Stop_Click(object sender, RoutedEventArgs e)
        {
            if(clientThread != null)
            {
                bSend = false;
                if (!clientThread.Join(1000))
                    clientThread.Abort();
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
        }

        private void bn_Send_Click(object sender, RoutedEventArgs e)
        {
            string strMsg = tb_Message.Text;
            byte[] buffer = Encoding.ASCII.GetBytes(strMsg);

            if(stream != null)
                stream.Write(buffer, 0, buffer.Length);
            list_Message.Items.Add(DateTime.Now.ToString("[yyyymmdd_hh.mm.ss]") + strMsg);
        }

        private void tb_IP_TextChanged(object sender, TextChangedEventArgs e)
        {
            strIP = tb_IP.Text;
        }

        private void tb_Port_TextChanged(object sender, TextChangedEventArgs e)
        {
            strPort = tb_Port.Text;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            bn_Stop_Click(null, null);
        }
    }
}
