using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LRM
{
    // Class for reading data asynchronously
    class StateObject
    {
        public static readonly int BufferSize = 1024;
        public byte[] buffer;
        public StringBuilder receivedData = new StringBuilder();
        public Socket tempSocket = null;

        public StateObject() => buffer = new byte[BufferSize];
    }

    // Server that stands for the cable cloud which communicates with routers
    class Listener
    {
        //This field controls a thread
        private ManualResetEvent allDone;

        //Address, port and endPoint
        private IPAddress address;
        private IPEndPoint localEndPoint;
        private int port;

        //Dictionary that stores connected sockets and thier names
        public static Dictionary<string, Socket> ClientSockets;


        //LRM
        public LinkResourceManager lrm;


        public Listener()
        {
            allDone = new ManualResetEvent(false);
            ClientSockets = new Dictionary<string, Socket>();
            lrm = new LinkResourceManager();
        }


        //Start the server
        public void Start()
        {
            address = IPAddress.Parse(Config.ADDRESS);
            port = Config.PORT;
            localEndPoint = new IPEndPoint(address, port);
            Socket listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult asyncResult)
        {
            allDone.Set();
            Socket tempListener = (Socket)asyncResult.AsyncState;
            Socket handler = tempListener.EndAccept(asyncResult);

            StateObject dataReader = new StateObject();
            dataReader.tempSocket = handler;

            handler.BeginReceive(dataReader.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), dataReader);
            tempListener.BeginAccept(new AsyncCallback(AcceptCallback), tempListener);
        }

        public void ReadCallback(IAsyncResult asyncResult)
        {
            StateObject dataReader = (StateObject)asyncResult.AsyncState;
            Socket handler = dataReader.tempSocket;

            string content = string.Empty;
            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(asyncResult);
            }
            catch (Exception e)
            {
                var router = ClientSockets.First(x => x.Value == handler);
                AddLog($"{router.Key} has been shutdown", ConsoleColor.Red);
                ClientSockets.Remove(router.Key);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }

            dataReader.receivedData.Clear();

            if (bytesRead > 0)
            {
                byte[] bytesReceived = new byte[bytesRead];
                Array.Copy(dataReader.buffer, bytesReceived, bytesRead);
                Commute(handler, dataReader, bytesReceived);
            }
            handler.BeginReceive(dataReader.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), dataReader);
        }


        //Handling the request
        public void Commute(Socket socket, StateObject state, byte[] beam)
        {
            string message = Encoding.ASCII.GetString(beam).Trim();
            string[] parts = message.Split("-");
            if (parts[0].Equals("HELLO"))
            {
                SaveSocket(socket, parts[1]);
                AddLog(message, ConsoleColor.Cyan);
            }
            else if (parts[0].Equals("LINK_CONNECTION_REQUEST"))
            {
                string[] nodes = parts[2].Split("&");
                if (parts.Length == 3)
                {
                    try
                    {
                        (double f1, double f2, int i1, int i2) = lrm.ChooseLinks(Convert.ToInt32(parts[1]), parts[2]);
                        for (int i = 0; i < nodes.Length; i++) 
                        {
                            AddLog($"LRM> [Link Connection Request]: Received request from CC", ConsoleColor.Gray);
                            AddLog($"LRM> Allocated {parts[1]} slot(s) at link {nodes[i].Replace("CC_", "")} - {nodes[i+1].Replace("CC_", "")} -> Frequency band: {f1.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)} to {f2.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)} [THz]", ConsoleColor.Magenta);
                            if (i == nodes.Length - 2)
                                break;
                        }
                        ResposneToCC(socket, f1, f2);
                        Thread.Sleep(500);
                    }
                    catch (Exception)
                    {
                        (double freq1, double freq2, _, _) = lrm.GoBackwards(Convert.ToInt32(parts[1]));
                        ResposneToCC(socket, freq1, freq2);

                    }
                }
                else
                {
                    lrm.UseFixedBand(parts[3], parts[4], parts[2]);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        AddLog($"LRM> [Link Connection Request]: Received request from CC", ConsoleColor.Gray);
                        AddLog($"LRM> Allocated {parts[1]} slot(s) at link {nodes[i].Replace("CC_", "")} - {nodes[i + 1].Replace("CC_", "")} -> Frequency band: {parts[3].Replace(",", ".")} to {parts[4].Replace(",", ".")} [THz]", ConsoleColor.Magenta);
                        if (i == nodes.Length - 2)
                            break;
                    }
                    ResposneToCC(socket, Math.Round(Convert.ToDouble(parts[3]), 4), Math.Round(Convert.ToDouble(parts[4]), 4));
                }
            } 
            else if (parts[0].Equals("RELEASE"))
            {
                string[] nodes = parts[2].Split("&");
                for (int i = 0; i < nodes.Length; i++)
                {
                    lrm.ReleaseResources(parts[1].Split("&")[0], parts[1].Split("&")[1], nodes[i], nodes[i + 1]);
                    AddLog($"LRM> [Link Connection Release] Releasing resources at link = {nodes[i]} - {nodes[i + 1]}", ConsoleColor.DarkMagenta);
                    if (i == nodes.Length - 2)
                        break;
                }
            }
        }

        //Add sockets to dictionary
        public void SaveSocket(Socket socket, string address)
        {
            ClientSockets.TryAdd(address, socket);
        }


        //Logging info
        public void AddLog(object s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            if (s is string)
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]; {s}");
            Console.ResetColor();
        }

        public void ResposneToCC(Socket socket, double f1, double f2)
        {
            //double lambda1 = (3f / f2) * 10000;
            //double lambda2 = (3f / f1) * 10000;
            string response = $"LINK_CONNECTION_RESPONSE${f1}@{f2}";
            byte[] data = Encoding.ASCII.GetBytes(response);
            socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(Callback), socket);

        }

        private void Callback(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                handler = (Socket)ar.AsyncState;
                //Complete sending
                int bytesSend = handler.EndSend(ar);
                AddLog($"LRM> Sending back a response to CC, paramters = frequencies above", ConsoleColor.DarkCyan);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
        }
    }
}