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

namespace SocketClientWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int port = 11000;

        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        private static String response = String.Empty;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartClient();
        }
        public void StartClient()
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = ipHostInfo.AddressList[0];

                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                client.BeginConnect(ipEndPoint,
                    new AsyncCallback(ConnectCallback), client);

                // текст с textBox записываем в msg
                var msg = hostname.Text;
                // и отправляем на сервер
                Send(client, msg);
                CreateLog(DateTime.Now + "   "+ msg);
                sendDone.WaitOne();

                Receive(client);
                receiveDone.WaitOne();
                // ответ с сервера записываем в другой textBox
                forResponceText.Text += $"{DateTime.Now} Response received : {response} \n";

                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception ex)
            {
                CreateLog(DateTime.Now + ex.ToString());                
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                client.EndConnect(ar);
                forResponceText.Text += $"Socket connected to {client.RemoteEndPoint.ToString()}. Time: {DateTime.Now}";

                connectDone.Set();
            }
            catch (Exception ex)
            {
                CreateLog(DateTime.Now + ex.ToString());
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = client;

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                CreateLog(DateTime.Now + ex.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                    var test = state.sb.ToString();

                    Console.ReadKey();
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }

                    receiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                CreateLog(DateTime.Now + ex.ToString());
            }
        }

        private void Send(Socket client, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                int bytesSent = client.EndSend(ar);
                Console.WriteLine($"Sent {bytesSent} bytes to server. Time: {DateTime.Now}");

                sendDone.Set();
            }
            catch (Exception ex)
            {
                CreateLog(DateTime.Now + ex.ToString());
            }
        }

        public void CreateLog(string logMessage)
        {
            try
            {
                string currentFilePath = Directory.GetCurrentDirectory() + "/logs.txt";
                FileStream fs = new FileStream(currentFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                byte[] data = Encoding.UTF8.GetBytes(logMessage);
                fs.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
