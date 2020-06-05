using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.Windows.Threading;

namespace SocketServer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpListener tcpListener;
        Thread tcpThread;
        List<TcpClient> listclient = new List<TcpClient>();
        List<Thread> listReadThread = new List<Thread>();
        bool bListener = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 클라이언트 Accept Thread
        /// </summary>
        private void TcpServerRun()
        {
            int nPort = 21000;
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
            tcpListener = new TcpListener(IPAddress.Any, nPort);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                tb_IP.Text = IPAddress.Any.ToString();
                tb_Port.Text = nPort.ToString();
            }));
            bListener = true;
            tcpListener.Start();
            
            while (bListener)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    listclient.Add(client);
                    listReadThread.Add(new Thread(new ParameterizedThreadStart(ClientRecive)));
                    listReadThread[listReadThread.Count - 1].Start(client);
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        list_Client.Items.Add(client.Client.LocalEndPoint.ToString());
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
                
            }
        }

        /// <summary>
        /// Client Data Recive Thread
        /// 접속된 클라이언트 수 만큼 표기.
        /// </summary>
        /// <param name="client">TCP Client Type</param>
        private void ClientRecive(Object client)
        {
            TcpClient mClient = client as TcpClient;
            NetworkStream stream = mClient.GetStream();
            byte[] message = new byte[1024];
            string strMsg;
            try
            {
                while (true)
                {
                    Thread.Sleep(10);
                    if (stream.DataAvailable)
                    {
                        stream.Read(message, 0, message.Length);
                        strMsg = Encoding.ASCII.GetString(message);
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                        {
                            list_Message.Items.Add(DateTime.Now.ToString("[yyyymmdd_hh.mm.ss]") + strMsg);
                        }));
                    }
                }
            }
            catch (ThreadAbortException ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                stream.Close();
                mClient.Close();
            }
        }

        /// <summary>
        /// Client Thread Stop
        /// </summary>
        private void fnStopThread()
        {
            if (listReadThread.Count > 0)
            {
                foreach (var thread in listReadThread)
                {
                    if(!thread.Join(1000))
                        thread.Abort();
                }
            }
            //             if (listclient.Count > 0)
            //             {
            //                 foreach (var client in listclient)
            //                 {
            //                     client.Close();
            //                 }
            //             }
        }

        #region Control Event
        private void bn_Run_Click(object sender, RoutedEventArgs e)
        {
            tcpThread = new Thread(new ThreadStart(TcpServerRun));
            tcpThread.Start();
        }

        private void bn_Stop_Click(object sender, RoutedEventArgs e)
        {
            bListener = false;
            if(tcpListener != null)
                tcpListener.Stop();
            if (tcpThread != null)
            {
                if (!tcpThread.Join(1000))
                    tcpThread.Abort();
            }

            fnStopThread();
        }

        private void bn_Send_Click(object sender, RoutedEventArgs e)
        {
            string strMsg = tb_Message.Text;
            byte[] buff = Encoding.ASCII.GetBytes(strMsg);

            if (listclient.Count > 0)
            {
                // 연결되있는 클라이언트 전체에 데이터 전송.
                foreach (var client in listclient)
                {
                    client.GetStream().Write(buff, 0, buff.Length);
                }
                list_Message.Items.Add(DateTime.Now.ToString("[yyyymmdd_hh.mm.ss]") + strMsg);
            }
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            fnStopThread();
            if(tcpThread != null)
            {
                tcpListener.Stop();
                if (!tcpThread.Join(1000))
                    tcpThread.Abort();
            }
                
        }
    }
}
