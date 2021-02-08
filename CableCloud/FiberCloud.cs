using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Commons.Tools;

namespace CableCloud
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
    class FiberCloud
    {
        //This field controls a thread
        private ManualResetEvent allDone;
        
        //Address, port and endPoint
        private IPAddress address;
        private IPEndPoint localEndPoint;
        private int port;

        //Dictionary that stores connected sockets and thier names
        public static Dictionary<string, Socket> ClientSockets;
       
        //Routing table that store forwarding info
        public RoutingTable routeTable;

        public FiberCloud()
        {
            routeTable = new RoutingTable();
            allDone = new ManualResetEvent(false);
            ClientSockets = new Dictionary<string, Socket>();
        }


        //Start the server
        public void StartCloud() {
            address = IPAddress.Parse(ConfigCloud.ADDRESS);
            port = ConfigCloud.PORT;
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
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult asyncResult)
        {
            allDone.Set();
            Socket tempListener = (Socket) asyncResult.AsyncState;
            Socket handler = tempListener.EndAccept(asyncResult);

            StateObject dataReader = new StateObject();
            dataReader.tempSocket = handler;

            handler.BeginReceive(dataReader.buffer, 0, StateObject.BufferSize, 0, 
                new AsyncCallback(ReadCallback), dataReader);
            tempListener.BeginAccept(new AsyncCallback(AcceptCallback), tempListener);
        }

        public void ReadCallback(IAsyncResult asyncResult)
        {
            StateObject dataReader = (StateObject) asyncResult.AsyncState;
            Socket handler = dataReader.tempSocket;

            string content = string.Empty;
            int bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(asyncResult);
            }
            catch(Exception e)
            {
                var router = ClientSockets.First(x => x.Value == handler);
                AddLog($"{router.Key} has been shutdown", ConsoleColor.Red);
                ClientSockets.Remove(router.Key);
                RoutingTable.HandleAdjacency(router.Key, 0);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }

            dataReader.receivedData.Clear();

            if(bytesRead > 0)
            {
                byte[] bytesReceived = new byte[bytesRead];
                Array.Copy(dataReader.buffer, bytesReceived, bytesRead);
                Commute(handler, dataReader, bytesReceived);
            }
            handler.BeginReceive(dataReader.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), dataReader);
        }
        
        public void Send(Socket handler, StateObject st, ImpulseSignalPackage data)
        {
            //Convert to bits
            byte[] byteData = data.ToBytes();
            //Begin sending data to client
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);

        }

        public void SendCallback(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                handler = (Socket) ar.AsyncState;
                //Complete sending
                int bytesSend = handler.EndSend(ar);
                AddLog($"Optical signal has been forwarded to the next node.", ConsoleColor.Green);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
        }

        //Handling the light beam
        public void Commute(Socket socket, StateObject state, byte[] beam)
        {
            ImpulseSignalPackage signal = null;
            try
            {
                signal = ImpulseSignalPackage.FromBytes(beam);
                AddLog(signal, ConsoleColor.Yellow);
                ForwardPackage(socket, state, signal);
            }
            catch (Exception e)
            {
                string message = Encoding.ASCII.GetString(beam).Trim();
                string[] parts = message.Split("-");
                if (parts[0].Equals("HELLO"))
                {
                    SaveSocket(socket, parts[1]);
                    AddLog(message, ConsoleColor.Cyan);
                }
            }
  
        }
        //Add sockets to dictionary
        public void SaveSocket(Socket socket, string address)
        {
            string nodeName = null;
            if (!address.StartsWith("RC") && !address.StartsWith("CC"))
            {
                nodeName = ConfigCloud.GetNodeName(IPAddress.Parse(address));
                ClientSockets.TryAdd(nodeName, socket);
                RoutingTable.HandleAdjacency(nodeName, 1);
            }
            else
            {
                nodeName = address;
                ClientSockets.TryAdd(nodeName, socket);
            }
            
        }

        //Forward packet to the next node
        public void ForwardPackage(Socket s, StateObject state, ImpulseSignalPackage signal)
        {
            string nodeName = ConfigCloud.GetNodeName(signal.PrevIP);
            string nextHop = routeTable.GetNextHop(nodeName, signal.Port.ToString());

            if (!nextHop.Equals("DISCARD"))
            {
                try
                {
                    s = ClientSockets.First(x => x.Key == routeTable.GetNextHop(nodeName, signal.Port.ToString())).Value;
                    signal.Port = Convert.ToUInt16(routeTable.GetNextPort(nodeName, signal.Port.ToString()));
                    Send(s, state, signal); //Sending signal to the next hop
                }
                catch(Exception e)
                {
                    AddLog("Not able to find next hop - loss of signal", ConsoleColor.Red);
                }
            }
            else
            {
                AddLog("Not able to find next hop - loss of signal",ConsoleColor.Red);
            }
        }

        //Logging info
        public void AddLog(object s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            if (s is string)
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]; {s}");
            else
            {
                ImpulseSignalPackage package = s as ImpulseSignalPackage;
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]; PORT: {package.Port}; FROM: {package.PrevIP}; PAYLOAD: {package.Payload}");
            }
            Console.ResetColor();
        }

    }
}
