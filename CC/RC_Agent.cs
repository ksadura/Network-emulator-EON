using System;
using Commons.Tools;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CC
{
    public class RC_Agent
    {
        private static IPAddress RC_IP { get; set; }
        private static ushort RC_Port { get; set; }
        public static Socket established_socket { get; set; }
        public static RouteTableQuery ROUTE_TABLE_QUERY { get; set; }

        public RC_Agent(Config config)
        {
            RC_IP = config.RCConnectionData.ipAddress;
            RC_Port = config.RCConnectionData.port;
        }
        public static void sendMessage(RouteTableQuery request)
        {
            if (established_socket == null || !established_socket.Connected)
            {
                AddLog("Connection to RC not established", LogType.Error);
                return;
            }
            try
            {
                established_socket.Send(request.ToBytes());
                AddLog($"RouteTableQuery_req send to RC: {request.TableQuery_Information()}", LogType.Information);
                var t = Task.Run(() => listenMessage());
                t.Wait();
            }
            catch (Exception e)
            {
                AddLog($"Unable to send information: {e.Message}", LogType.Error);
            }

        }
        public static void listenMessage()
        {
            while (true)
            {
                while (established_socket == null || !established_socket.Connected)
                {
                    AddLog("Establishing connection with RC", LogType.Information);
                    EstablishRCConnection();
                }
                try
                {
                    var buffer = new byte[256];
                    int bytes = established_socket.Receive(buffer);
                    byte[] receivedBytes = new byte[bytes];
                    Array.Copy(buffer, receivedBytes, bytes);
                    RouteTableQuery response = RouteTableQuery.FromBytes(receivedBytes);
                    if (response != null)
                    {
                        AddLog($"RouteTableQuery_rsp received from RC: {response.TableQuery_Information()}", LogType.Information);
                        ROUTE_TABLE_QUERY = response;
                        break;
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            AddLog("Connection with RC was broken!", LogType.Error);
                            continue;
                        }
                        else
                        {
                            AddLog("Unable to connect with RC!", LogType.Error);
                        }
                    }
                }

            }
        }
        public void clearPath()
        {
            ROUTE_TABLE_QUERY = null;
        }
        public static Socket EstablishRCConnection()
        {
            AddLog($"Connecting with RC: {RC_IP}:{RC_Port}", LogType.Information);
            try
            {
                established_socket = new MessageSocket(RC_IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                established_socket.Connect(new IPEndPoint(RC_IP, RC_Port));
                AddLog("Connection with RC was established", LogType.Information);
            }
            catch (Exception)
            {
                AddLog("Unable to establish connection with RC", LogType.Error);
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
