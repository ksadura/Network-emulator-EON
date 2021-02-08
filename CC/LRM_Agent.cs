using System;
using Commons.Tools;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CC
{
    public class LRM_Agent
    {
        private IPAddress LRM_IP { get; set; }
        private ushort LRM_Port { get; set; }
        public static Socket established_socket { get; set; }
        public string[] lambda_range { get; set; }

        public LRM_Agent(Config config)
        {
            LRM_IP = config.LRMConnectionData.ipAddress;
            LRM_Port = config.LRMConnectionData.port;
        }
        public static void sendMessage(string request)
        {
            if (established_socket == null || !established_socket.Connected)
            {
                AddLog("Connection to LRM not established", LogType.Error);
                return;
            }
            try
            {
                established_socket.Send(Encoding.ASCII.GetBytes(request));
                string[] splittedReq = request.Split("-");
                string[] nodes = splittedReq[2].Split("&");
                if (splittedReq.Length > 3)
                {
                    for (int i = 0; i < nodes.Length - 1; i++)
                    {
                        AddLog($"Send link connection request to LRM on link {nodes[i]}-{nodes[i + 1]}, Slots amount: {splittedReq[1]}, Frequency range: {splittedReq[3].Replace(",", ".")} - {splittedReq[4].Replace(",", ".")} [THz]", LogType.Information);
                    }
                } else
                {
                    for (int i = 0; i < nodes.Length - 1; i++)
                    {
                        AddLog($"Send link connection request to LRM on link {nodes[i]}-{nodes[i + 1]}, Slots amount: {splittedReq[1]}", LogType.Information);
                    }
                }
            }
            catch (Exception e)
            {
                AddLog($"Unable to send request: {e.Message} {e.StackTrace}", LogType.Error);
            }

        }

        public static void sendReleaseMessage(string request)
        {
            if (established_socket == null || !established_socket.Connected)
            {
                AddLog("Connection to LRM not established", LogType.Error);
                return;
            }
            try
            {
                established_socket.Send(Encoding.ASCII.GetBytes(request));
                string[] splittedReq = request.Split("-");
                string[] nodes = splittedReq[2].Split("&");
                string[] lambda_range = splittedReq[1].Split("&");
                for (int i = 0; i < nodes.Length - 1; i++)
                {
                    AddLog($"Send release request to LRM on link {nodes[i]}-{nodes[i + 1]}, Frequency range: {lambda_range[0].Replace(",", ".")} - {lambda_range[1].Replace(",", ".")} [THz]", LogType.Information);
                }
            }
            catch (Exception e)
            {
                AddLog($"Unable to send request: {e.Message}", LogType.Error);
            }
        }
        public void listenMessage()
        {
            while (true)
            {
                while (established_socket == null || !established_socket.Connected)
                {
                    AddLog("Establishing connection with LRM", LogType.Information);
                    EstablishLRMConnection();
                }
                try
                {
                    var buffer = new byte[256];
                    int bytes = established_socket.Receive(buffer);
                    byte[] receivedBytes = new byte[bytes];
                    Array.Copy(buffer, receivedBytes, bytes);
                    string response = Encoding.ASCII.GetString(receivedBytes);
                    if (response != null)
                    {
                        string[] splittedResponse = response.Split("$");
                        switch (splittedResponse[0])
                        {
                            case "LINK_CONNECTION_RESPONSE":
                                if (!CC.isSubCC)
                                {
                                    AddLog($"Link connection response received from LRM", LogType.Information);
                                    lambda_range = handleResponse(splittedResponse[1]);
                                }
                                break;
                            default:
                                string res = response;
                                if (res.Split("-").Length > 0)
                                {
                                    if (res.Split("-")[0] == "ERROR")
                                    {
                                        CC.reconfigure(res);
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            AddLog("Connection with LRM was broken!", LogType.Error);
                            continue;
                        }
                        else
                        {
                            AddLog("Unable to connect with LRM!", LogType.Error);
                        }
                    }
                }

            }
        }

        private string[] handleResponse(string response)
        {
            return response.Split("@");
        }

        public void clearLambda()
        {
            lambda_range = null;
        }
        public Socket EstablishLRMConnection()
        {
            AddLog($"Connecting with LRM: {LRM_IP}:{LRM_Port}", LogType.Information);
            try
            {
                established_socket = new MessageSocket(LRM_IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                established_socket.Connect(new IPEndPoint(LRM_IP, LRM_Port));
                established_socket.Send(Encoding.ASCII.GetBytes($"HELLO-{Config.Name}"));
                AddLog("Connection with LRM was established", LogType.Information);
                var t = Task.Run(() => listenMessage());
                t.Wait();
            }
            catch (Exception)
            {
                AddLog("Unable to establish connection with LRM", LogType.Error);
            }
            return established_socket;

        }
        private static void AddLog(string log, LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogType.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogType.Received:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
            }
            log = $"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {log}";
            Console.WriteLine(log);
        }
    }
}
