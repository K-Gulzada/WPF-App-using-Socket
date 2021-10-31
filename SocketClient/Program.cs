using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    public class AsynchronousClient
    {
        private const int port = 11000;

        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        private static String response = String.Empty;

        private static void StartClient()
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
                connectDone.WaitOne();
                Console.WriteLine("Enter the message");
                var msg = Console.ReadLine();

                Send(client, msg);
                sendDone.WaitOne();

                Receive(client);
                receiveDone.WaitOne();

                Console.WriteLine($"Response received : {response} Time: {DateTime.Now}");
                Console.ReadKey();
                
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                client.EndConnect(ar);

                Console.WriteLine($"Socket connected to {client.RemoteEndPoint.ToString()}. Time: {DateTime.Now}");

                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = client;

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            { 
                Socket client = (Socket)ar.AsyncState;
 
                int bytesSent = client.EndSend(ar);
                Console.WriteLine($"Sent {bytesSent} bytes to server. Time: {DateTime.Now}");

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static int Main(String[] args)
        {
            StartClient();
            return 0;
        }
    }
}
