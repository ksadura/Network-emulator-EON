using System;
using Commons.Tools;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NCC
{
    public class CC_Agent
    {
        private IPAddress CC_IP { get; set; }
        private ushort CC_Port { get; set; }
        public MessagePackage messageToNCC { get; set; }
        public static MessageSocket established_socket { get; set; }

        public CC_Agent(NCCConfig config)
        {
            CC_IP = config.CC_IP;
            CC_Port = config.CC_Port;
            messageToNCC = null;
        }
        public void sendMessage(IPAddress Source,IPAddress Destination, string payload, double desired_speed,float power,int ID)
        {
            if (established_socket == null || !established_socket.Connected)
            {
                AddLogInfo("Brak połączenia z CC przesyłanie informacji CPCC");
                return;
            }

            MessagePackage message_package = new MessagePackage();

            message_package.DestinationIP = Destination;
            message_package.Payload = payload;
            message_package.desired_bandwidth = desired_speed;
            message_package.SourceIP = Source;
            message_package.power_level = power;
            message_package.call_ID = ID;
            try
            {
                established_socket.Send(message_package.ToBytes());
                AddLogInfo($"Wysłano informacje do CC: {message_package.Packet_Information()}");
            }
            catch (Exception e)
            {
                AddLogInfo($"Nie udało się wysłać informacji: {e.Message}");
            }

        }
        public void listenMessage()
        {
            while (true)
            {
                while (established_socket == null || !established_socket.Connected)
                {
                    AddLogInfo("Próba wznowienia połączenia z sterownikiem CC");
                    EstablishCCConnection();
                }

                try
                {
                    MessagePackage package = established_socket.Receive();

                    if (package != null)
                    {
                        AddLogInfo($"Otrzymano informacje od CC: {package.Packet_Information()}");
                        string[] message = package.Payload.Split("@");
                        switch (message[0])
                        {
                            case "Connection request_rsp":
                                InformNCC(package);
                                break;
                            case "Connection request_rsp(release)":
                                InformNCC(package);
                                break;
                            case "Connection request_rsp(release false)":
                                InformNCC(package);
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
                            AddLogInfo("Zerwano połączenie z sterownikiem CC!");
                            continue;
                        }

                        else
                        {
                            AddLogInfo("Nie udało się połączyć z sterownikiem CC!");
                        }
                    }
                }
                catch (Exception e)
                {
                    AddLogInfo(e.Message + e.StackTrace);
                }

            }
        }
        public void InformNCC(MessagePackage package)
        {
            messageToNCC = package;
        }
        public void clearNCC()
        {
            messageToNCC = null;
        }
        public MessageSocket EstablishCCConnection()
        {
            AddLogInfo($"Trwa łączenie z sterownikiem CC {CC_IP}:{CC_Port}");
            try
            {
                established_socket = new MessageSocket(CC_IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                established_socket.Connect(new IPEndPoint(CC_IP, CC_Port));
                established_socket.Send(Encoding.ASCII.GetBytes($"HELLO-NCC"));
                Task.Run(() => listenMessage());
                AddLogInfo("Zestawiono połączenie z sterownikiem CC");

            }
            catch (Exception)
            {
                AddLogInfo("Nie udało się połączyć z sterownikiem CC");
            }
            return established_socket;

        }
        public static void AddLogInfo(string info)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {info}");
            Console.ResetColor();
        }
    }
}
