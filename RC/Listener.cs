using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.Collections.Generic;
using Commons.Tools;
using System.Linq;

namespace Dijkstra
{
    //Class for reading data asynchronously
    class StateObjcet
    {
        public static readonly int BufferSize = 1024;
        public byte[] buffer;
        public StringBuilder receivedData = new StringBuilder();
        public Socket tempSocket = null;

        //Constructor
        public StateObjcet() => buffer = new byte[BufferSize];

    }

    //Class representing a listening agent in RC
    class Listener
    {
        //This variable controls a thread
        private ManualResetEvent allDone;

        //Address, port and end point
        private IPAddress address;
        private IPEndPoint localEndPoint;
        private int port;

        //LRM attributes
        private static IPAddress lrmIP = IPAddress.Parse("127.0.0.1");
        private static int lrmPort = Config.LRMPort;
        private ManualResetEvent _connectDone = new ManualResetEvent(false);
        private ManualResetEvent _helloDone = new ManualResetEvent(false);
        private ManualResetEvent _receivedDone = new ManualResetEvent(false);
        private bool check = false;


        //Topology's graph
        private Topology topology;
        private RoutingAgent routingAgent;

        public Listener()
        {
            allDone = new ManualResetEvent(false);
            topology = new Topology();
            routingAgent = new RoutingAgent();
        }

        public void StartRoutingController()
        {
            address = IPAddress.Parse(Config.Address);
            port = Config.Port;
            localEndPoint = new IPEndPoint(address, port);
            Socket listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Socket socketToLRM = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socketToLRM.BeginConnect(new IPEndPoint(lrmIP, lrmPort), new AsyncCallback(ConnectCallback), socketToLRM);
                _connectDone.WaitOne();
                SendHelloToLRM(socketToLRM);
                _helloDone.WaitOne();

                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    allDone.Reset();
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();

                    _receivedDone.Reset();
                    Receive(socketToLRM);
                    _receivedDone.WaitOne();

                }
            }
            catch(Exception e){
                Console.WriteLine(e.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            Socket tempListener = ar.AsyncState as Socket;
            Socket handler = tempListener.EndAccept(ar);

            StateObjcet dataReader = new StateObjcet();
            dataReader.tempSocket = handler;

            handler.BeginReceive(dataReader.buffer, 0, StateObjcet.BufferSize, 0,
                new AsyncCallback(ReadCallback), dataReader);
            tempListener.BeginAccept(new AsyncCallback(AcceptCallback), tempListener);
        }

        private void ReadCallback(IAsyncResult ar) 
        {
            StateObjcet dataReader = ar.AsyncState as StateObjcet;
            Socket handler = dataReader.tempSocket;

            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Connection's broken");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }

            dataReader.receivedData.Clear();
            if(bytesRead > 0)
            {
                byte[] bytesReceived = new byte[bytesRead];
                Array.Copy(dataReader.buffer, bytesReceived, bytesRead);
                HandleRequest(handler, bytesReceived);
            }
            handler.BeginReceive(dataReader.buffer, 0, StateObjcet.BufferSize, 0,
                new AsyncCallback(ReadCallback), dataReader);
        }

        public void HandleRequest(Socket socket, byte[] request) 
        {
            List<string> shortestPath;
            Tuple<double, Dictionary<string,string>> tuple;
            RouteTableQuery rtq = null;
            try
            {
                rtq = RouteTableQuery.FromBytes(request);
                string src = Config.ipToName[rtq.SourceIP.ToString()];
                string dst = Config.ipToName[rtq.DestinationIP.ToString()];
                AddLog($"[Route Table Query (request)]: Received query from CC, parameters: src = {src}; dst = {dst}", ConsoleColor.Magenta);
                tuple = topology.Dijkstra(src, dst, rtq.Lambda_Amount);
                shortestPath = topology.FindPath(tuple.Item2, dst);
                int number = routingAgent.CalculateSlots(rtq.bandwith, src, dst);
                rtq.Lambda_Amount = number;
                SendResponse(socket, rtq, shortestPath);
            }
            catch (Exception e)
            {
                string[] info = Encoding.ASCII.GetString(request).Split(" ");
                switch (info[0])
                {
                    case "ERROR":
                        topology.RemoveLink(info[1].Split("&")[0], info[1].Split("&")[1]);
                        AddLog($"[Local Topology]: LRM informed about topology's change -> Link {info[1].Split("&")[0]} - {info[1].Split("&")[1]} has been removed", ConsoleColor.DarkRed);
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;

                }
            }
        }

        public void SendResponse(Socket socket, RouteTableQuery rtq, List<string> response) 
        {
            try
            {
                //Unpacking array
                string[] result = topology.ConcatenatePorts(response).Split(" ");
                result = result.Where(val => !val.Contains("H")).ToArray();
                int i = 0;

                foreach (string s in result)
                {
                    RouteTableRow row = new RouteTableRow();
                    if (s == result[0] && rtq.subPortIn != 0)
                    {
                        row.PortIn = rtq.subPortIn;
                        row.NodeName = s.Split("-")[0];
                        row.PortOut = Convert.ToUInt16(s.Split("-")[1].Replace("(", "").Replace(")", ""));
                        rtq.RouteTableRows.Add(row);
                    }
                    else if (s == result[result.Length - 1] && rtq.subPortOut != 0)
                    {
                        row.PortIn = Convert.ToUInt16(s.Split("-")[0].Replace("(", "").Replace(")", ""));
                        row.NodeName = s.Split("-")[1];
                        row.PortOut = rtq.subPortOut;
                        rtq.RouteTableRows.Add(row);
                    }
                    else
                    {
                        row.PortIn = Convert.ToUInt16(s.Split("-")[0].Replace("(", "").Replace(")", ""));
                        row.NodeName = s.Split("-")[1];
                        row.PortOut = Convert.ToUInt16(s.Split("-")[2].Replace("(", "").Replace(")", ""));
                        rtq.RouteTableRows.Add(row);
                    }
                    i += 1;
                }

                //Convering to bits
                byte[] data = rtq.ToBytes();

                socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), socket);
                AddLog($"[Route Table Query (response)]: Selected path = { string.Join(" ", result).Replace("CC_", "")}", ConsoleColor.White);
            }
            catch (Exception e)
            {
                AddLog(e.Message, ConsoleColor.DarkYellow);
            }
            
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                handler = ar.AsyncState as Socket;
                //Complete sending
                int bytesSend = handler.EndSend(ar);
                Console.WriteLine("Sending response to CC...");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static void AddLog(object s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            if (s is string)
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]; {s}");
            Console.ResetColor();
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = ar.AsyncState as Socket;

                client.EndConnect(ar);

                Console.WriteLine("Established connection with Link Resource Manager");
                _connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendHelloToLRM(Socket socket)
        {
            string data = "HELLO-";
            byte[] byteData = Encoding.ASCII.GetBytes(data + Config.Name);
            socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallbackLRM), socket);
        }

        private void SendCallbackLRM(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                handler = ar.AsyncState as Socket;
                //Complete sending
                int bytesSend = handler.EndSend(ar);
                Console.WriteLine("Hello has been sent to LRM");
                _helloDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Receive(Socket socket)
        {
            try
            {
                StateObjcet state = new StateObjcet();
                state.tempSocket = socket;
                check = true;
                socket.BeginReceive(state.buffer, 0, StateObjcet.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar) 
        {
            StateObjcet state = ar.AsyncState as StateObjcet;
            Socket handler = state.tempSocket;

            int amount = 0;

            try
            {
                amount = handler.EndReceive(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Connection's broken");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }

            byte[] array = new byte[amount];
            Buffer.BlockCopy(state.buffer, 0, array, 0, amount);
            AddLog("Received message from LRM about change", ConsoleColor.DarkMagenta);
            HandleRequest(handler, array);
            handler.BeginReceive(state.buffer, 0, StateObjcet.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
            _receivedDone.Set();
            
        }




    }
}
