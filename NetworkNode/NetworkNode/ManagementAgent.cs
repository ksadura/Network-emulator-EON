using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Commons.Tools;
using static NetworkNodes.NetworkNode;


namespace NetworkNodes
{
    public class ManagementAgent
    {
        public NetworkNodeConfig Config { get; set; }

        public Socket ConnectedSocket { get; set; }

        public NetworkNodeRoutingTables RoutingTable { get; set; }

        private ManualResetEvent mre = new ManualResetEvent(false);

        public ManagementAgent(NetworkNodeConfig networkNodeConfig)
        {
            Config = networkNodeConfig;
            RoutingTable = new NetworkNodeRoutingTables();
        }

        public void StartTask()
        {
            Task.Run(() => Start());
        }

        public void Start()
        {
            while (true)
            {
                ConnectToCC();

                if (ConnectedSocket == null)
                {
                    continue;
                }

                while (true)
                {
                    try
                    {
                        var buffer = new byte[256];
                        int bytes = ConnectedSocket.Receive(buffer);
                        byte[] receivedBytes = new byte[bytes];
                        Array.Copy(buffer, receivedBytes, bytes);
                        RouteTableRow package = RouteTableRow.FromBytes(receivedBytes);
                        Task.Run(() => HandleMessage(package));
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode != SocketError.TimedOut)
                        {
                            if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                AddLog("Connection to CC broken!", LogType.Error);
                                break;
                            }

                            else
                            {
                                AddLog($"{e.Source}: {e.SocketErrorCode}", LogType.Error);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        AddLog(e.Message + e.StackTrace, LogType.Error);
                    }
                }
            }
        }

        private void ConnectToCC()
        {
            AddLog($"Connecting to CC at {Config.CCAddress}:{Config.CCPort}", LogType.Information);
            while (true)
            {
                Socket socket = new Socket(Config.CCAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveTimeout = 20000;

                try
                {
                    var result = socket.BeginConnect(new IPEndPoint(Config.CCAddress, Config.CCPort), null, null);

                    bool success = result.AsyncWaitHandle.WaitOne(5000, true);
                    if (success)
                    {
                        socket.EndConnect(result);
                    }
                    else
                    {
                        socket.Close();
                        AddLog("Connection to CC not established - timeout...", LogType.Error);
                        continue;
                    }
                }
                catch (Exception)
                {
                    AddLog("Retrying...", LogType.Information);
                }

                try
                {
                    AddLog($"Sending hello to CC...", LogType.Information);
                    socket.Send(Encoding.ASCII.GetBytes($"HELLO-{NetworkNodeConfig.NodeName}"));
                    AddLog("Estabilished connection with CC", LogType.Information);
                    ConnectedSocket = socket;
                    break;
                }
                catch (Exception)
                {
                    AddLog("Couldn't connect to CC!", LogType.Error);
                }
            }
        }


        private void HandleMessage(RouteTableRow package)
        {
            string tmp_message = null;
            switch (package.action)
            {
                case "ADD":
                    tmp_message = RoutingTable.AddTableEntry(package);
                    break;
                case "DEL":
                    tmp_message = RoutingTable.DeleteTableEntry(package);
                    break;
                default:
                    AddLog(package.TableRow_Information(),LogType.Information);
                    break;
            }
            if (tmp_message != null)
            {
                ConnectedSocket.Send(Encoding.ASCII.GetBytes(tmp_message));
                AddLog($"Sending {tmp_message} to CC", LogType.Information);
            }
        }

        private void AddLog(string log, LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogType.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            log = $"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {log}";
            Console.WriteLine(log);
        }
    }
}
