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
using System.IO;

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

        private void SetKeepAlive(Socket sock)
        {
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            MemoryStream ms = new MemoryStream(sizeof(uint) * 3);
            ms.Write(BitConverter.GetBytes((int)1), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes((int)2000), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes((int)1000), 0, sizeof(int));
            sock.IOControl(IOControlCode.KeepAliveValues, ms.GetBuffer(), BitConverter.GetBytes(0));
            ms.Close();
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
                SetKeepAlive(client.Client);
                client.Connect(IPAddress.Parse(strIP), Convert.ToInt32(strPort));
                stream = client.GetStream();

                byte[] message = new byte[nMaxBufferCount];
                byte[] check = { 0xFF };
                string strMsg;
                int byteRead;
               
                while (bSend)
                {
                    if (stream.DataAvailable)
                    {
                        byteRead = stream.Read(message, 0, message.Length);

                        if (message[0] == 0xFF) continue;

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
            bn_Conn.IsEnabled = false;
            bn_DisConn.IsEnabled = true;
            bn_Send.IsEnabled = true;
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
            bn_Conn.IsEnabled = true;
            bn_DisConn.IsEnabled = false;
            bn_Send.IsEnabled = false;
        }

        private void bn_Send_Click(object sender, RoutedEventArgs e)
        {
            string strMsg = tb_Message.Text;
            byte[] buffer = new byte[1024];
            byte[] msg = Encoding.ASCII.GetBytes(strMsg);
            Array.Copy(msg, buffer, msg.Length);

            if (stream != null)
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
