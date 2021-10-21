using System;
using System.Collections.Generic;
using System.IO;
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
        struct ST_CLIENT
        {
            public string endpoint;
            public Thread thread;
            public TcpClient tcpclient;
        }
        TcpListener tcpListener;
        Thread tcpThread;
        List<ST_CLIENT> listclient = new List<ST_CLIENT>();
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
            
            SetKeepAlive(tcpListener.Server);

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
                    ST_CLIENT stClient = new ST_CLIENT();
                    stClient.tcpclient = tcpListener.AcceptTcpClient();
                    stClient.tcpclient.ReceiveTimeout = 1000;
                    stClient.endpoint = stClient.tcpclient.Client.RemoteEndPoint.ToString();
                    stClient.thread = new Thread(new ParameterizedThreadStart(ClientRecive));
                    listclient.Add(stClient);
                    listReadThread.Add(stClient.thread);
                    listReadThread[listReadThread.Count - 1].Start(stClient);
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        list_Client.Items.Add(stClient.endpoint);
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
                
            }
        }

        private void SetKeepAlive(Socket sock)
        {
            MemoryStream ms = new MemoryStream(sizeof(uint) * 3);
            ms.Write(BitConverter.GetBytes((int)1), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes((int)2000), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes((int)1000), 0, sizeof(int));
            sock.IOControl(IOControlCode.KeepAliveValues, ms.GetBuffer(), BitConverter.GetBytes(0));
            ms.Close();
        }

        /// <summary>
        /// Client Data Recive Thread
        /// 접속된 클라이언트 수 만큼 표기.
        /// </summary>
        /// <param name="client">TCP Client Type</param>
        private void ClientRecive(object client)
        {
            ST_CLIENT stClient = (ST_CLIENT)client;
            NetworkStream stream = stClient.tcpclient.GetStream();
            byte[] message = new byte[1024];
            byte[] check = { 0xFF };
            int nChkCnt = 0;
            string strMsg;
            try
            {
                while (stClient.tcpclient.Connected)
                {
                    Thread.Sleep(10);
                    
                    // 연결 상태 갱신을 위해 1초에 한번씩 데이터 전송.
                    if (nChkCnt++ < 100)
                    {
                        stream.Write(check, 0, check.Length);
                        nChkCnt = 0;
                    }

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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                stream.Close();
                stClient.tcpclient.Close();
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                list_Client.Items.Remove(stClient.endpoint);
            }));
            listclient.Remove(stClient);
            listReadThread.Remove(stClient.thread);
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
        }

        #region Control Event
        private void bn_Run_Click(object sender, RoutedEventArgs e)
        {
            bn_Run.IsEnabled = false;
            bn_Stop.IsEnabled = true;
            bn_Send.IsEnabled = true;
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
            bn_Run.IsEnabled = true;
            bn_Stop.IsEnabled = false;
            bn_Send.IsEnabled = false;
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
                    client.tcpclient.GetStream().Write(buff, 0, buff.Length);
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
