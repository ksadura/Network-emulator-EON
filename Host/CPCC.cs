using System;
using Commons.Tools;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Host
{
    public class CPCC
    {
        private HostParseConfig hostInfo;
        private CPCCParseConfig cpccInfo;
        private string temp_DestinationName;
        private string temp_payload;
        private double temp_desired_speed;
        public static int call_ID = 1;
        public static MessageSocket established_socket { get; set; }
        public CPCC(HostParseConfig hostInfo,CPCCParseConfig cpccInfo)
        {
            this.hostInfo = hostInfo;
            this.cpccInfo = cpccInfo;
        }
        public void sendMessage(string Destination, string payload,double desired_speed, bool isPending, int ID)
        {
            if (established_socket == null || !established_socket.Connected)
            {
                AddErrorInfo("Brak połączenia z NCC! Informacja odrzucona");
                Host.AddErrorInfo("Wysylanie wiadomosci nie powiodlo sie, błąd połączenia");
                return;
            }

            MessagePackage message_package = new MessagePackage();

            message_package.DestinationName = Destination;
            message_package.DestinationIP = null;
            if (call_ID != 0)
            {
                message_package.call_ID = ID;
            }
            if (isPending)
            {
                message_package.Payload = payload;
                message_package.desired_bandwidth = desired_speed;
                if (desired_speed != 0)
                {
                    message_package.power_level = hostInfo.power_level;
                }
                else
                    message_package.power_level = 0;
            }
            else
            {
                temp_DestinationName = Destination;
                temp_desired_speed = desired_speed;
                temp_payload = payload;
                message_package.Payload = "Call request_req";
                message_package.call_ID = call_ID++;
                message_package.desired_bandwidth = desired_speed;
                message_package.power_level = 0;
            }
            message_package.SourceIP = null;
            message_package.SourceName = hostInfo.host_name;
            try
            {
                established_socket.Send(message_package.ToBytes());
                if (message_package.Payload != "Connection request" && message_package.Payload != "Call pending")
                {
                    AddLogInfo($"Wysłano informacje: {message_package.Packet_Information()}");
                }
            }
            catch (Exception e)
            {
                AddErrorInfo($"Nie udało się wysłać informacji: {e.Message}");
            }

        }
        public void sendDeleteMessage(string Destination, string payload, double desired_speed,int ID)
        {
            if (established_socket == null || !established_socket.Connected)
            {
                AddErrorInfo("Brak połączenia z NCC! Informacja odrzucona");
                Host.AddErrorInfo("Wysylanie wiadomosci nie powiodlo sie, błąd połączenia");
                return;
            }

            MessagePackage message_package = new MessagePackage();

            message_package.DestinationName = Destination;
            message_package.DestinationIP = null;
            message_package.Payload = payload;
            message_package.desired_bandwidth = desired_speed;
            message_package.call_ID = ID;
            if (desired_speed != 0)
            {
                message_package.power_level = hostInfo.power_level;
            }
            else
                message_package.power_level = 0;
            message_package.SourceIP = null;
            message_package.SourceName = hostInfo.host_name;
            try
            {
                established_socket.Send(message_package.ToBytes());
                if (message_package.Payload != "Connection request" && message_package.Payload != "Call pending")
                {
                    AddLogInfo($"Wysłano informacje: {message_package.Packet_Information()}");
                }
            }
            catch (Exception e)
            {
                AddErrorInfo($"Nie udało się wysłać informacji: {e.Message}");
            }

        }
        public void listenMessage()
        {
            while (true)
            {
                while (established_socket == null || !established_socket.Connected)
                {
                    AddLogInfo("Próba wznowienia połączenia z sterownikiem NCC");
                    EstablishNCCConnection();
                }

                try
                {
                    MessagePackage package = established_socket.Receive();

                    if (package != null)
                    {
                        if (package.Payload != "Call pending")
                        {
                            AddLogInfo($"Otrzymano informacje: {package.Packet_Information()}");
                        }
                        string[] message = package.Payload.Split("@");
                        switch (message[0])
                        {
                            case "Call Accept_req":
                                string temp = "Call Accept_rsp(" + hostInfo.IsReceiving.ToString() + ")";
                                sendMessage(package.SourceName,temp,0,true,package.call_ID);
                                break;
                            case "Call pending":
                                sendMessage(temp_DestinationName, "Connection request", temp_desired_speed, true, package.call_ID);
                                break;
                            case "Call request_rsp(refused)":
                                AddErrorInfo("Wysylanie informacji do hosta, że wybrany adres nie istnieje/nie przyjmuje połączeń");
                                Host.AddErrorInfo("Wysylanie wiadomosci nie powiodlo sie, adres nie istnieje/nie przyjmuje połączeń");
                                break;
                            case "Call request_rsp(closed)":
                                AddErrorInfo("Wysylanie informacji do hosta, że wystąpił błąd łączności");
                                Host.AddErrorInfo("Wysylanie wiadomosci nie powiodlo sie, błąd połączenia");
                                break;
                            case "Call release_rsp(closed)":
                                AddErrorInfo("Wysylanie informacji do hosta, że wystąpił błąd łączności podczas usuwania połączenia");
                                Host.AddErrorInfo("Usuwanie połączenia nie powiodlo sie, błąd łączności");
                                break;
                            case "Call release_rsp(refused)":
                                AddErrorInfo("Wysylanie informacji do hosta, że wybrany adres nie istnieje/nie ma z nim połączenia");
                                Host.AddErrorInfo("Usunięcie połączenia nie powiodlo sie, adres nie istnieje/nie ma z nim połączenia");
                                break;
                            case "Call request_rsp":
                                string[] lambdas = new string[2];
                                AddLogInfo("Wysylanie informacji do hosta, że połączenie zostało zestawione");
                                if (message.Length > 1)
                                {
                                    lambdas = message[1].Split("-");
                                }
                                else
                                {
                                    //Do usuniecia po implementacji lambd
                                    lambdas[0] = "1";
                                    lambdas[1] = "2";
                                }
                                Host.AddLogInfo($"Połączenie z {temp_DestinationName} zostało zestawione wpisz sendmessage, żeby wysłać wiadomość");
                                Host.AddConnection(lambdas, package.DestinationIP, temp_DestinationName, package.call_ID, package.desired_bandwidth);
                                break;
                            case "Call release_rsp":
                                AddLogInfo($"Wysylanie informacji do hosta, że usunięcie połączenia z {package.DestinationName} się powiodło");
                                Host.DeleteConnection(package.DestinationName,package.call_ID);
                                break;
                            case "Call release_rsp(false)":
                                AddLogInfo($"Wysylanie informacji do hosta, że usunięcie połączenia z {package.DestinationName} nie powiodło się - brak żądanej ścieżki");
                                Host.AddLogInfo($"Usunięcie połączenia z {package.DestinationName} nie powiodło się - brak żądanej ścieżki");
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
                            AddErrorInfo("Zerwano połączenie z sterownikiem NCC!");
                            continue;
                        }

                        else
                        {
                            AddErrorInfo("Nie udało się połączyć z sterownikiem NCC!");
                        }
                    }
                }

            }
        }
        public MessageSocket EstablishNCCConnection()
        {
            AddLogInfo($"Trwa łączenie z sterownikiem NCC {cpccInfo.NCC_IP}:{cpccInfo.NCC_port}");
            try
            {
                established_socket = new MessageSocket(cpccInfo.NCC_IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                established_socket.Connect(new IPEndPoint(cpccInfo.NCC_IP, cpccInfo.NCC_port));
                established_socket.Send(Encoding.ASCII.GetBytes($"HELLO-{hostInfo.IP}"));
                Task.Run(() => listenMessage());
                AddLogInfo("Zestawiono połączenie z sterownikiem NCC");

            }
            catch (Exception)
            {
                AddErrorInfo("Nie udało się połączyć z sterownikiem NCC");
            }
            return established_socket;

        }
        public void updatedStatus()
        {
            AddLogInfo($"Host zmienił status odbierania z {!hostInfo.IsReceiving} na {hostInfo.IsReceiving}");
        }

        public static void AddLogInfo(string info)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] CPCC: {info}");
            Console.ResetColor();
        }
        public static void AddErrorInfo(string info)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] CPCC: {info}");
            Console.ResetColor();
        }
    }
}
