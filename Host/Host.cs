using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commons.Tools;

namespace Host
{
    class Host
    {
        public static ImpulseSignalSocket established_socket { get; set; }
        public static int sent_packages = 0;
        public static List<OtherHostsInfo> call_list = new List<OtherHostsInfo>();
        static void Main(string[] args)
        {
            ImpulseSignalPackage impulse = new ImpulseSignalPackage();
            bool flag = true;
            string Handler;
            string[] Parameters = new string[2];
            HostParseConfig host = new HostParseConfig();
            CPCC cpcc = null;
            if (args.Length != 0) { host = HostParseConfig.LoadFromConfigFile(args[1]); cpcc = new CPCC(host, new CPCCParseConfig(args[1])); }
            else { Console.WriteLine("Błędna śćieżka do pliku konfiguracyjnego"); host = HostParseConfig.LoadFromConfigFile("./HostConfigExample.xml"); cpcc = new CPCC(host, new CPCCParseConfig("./HostConfigExample.xml")); }
            Console.Clear();
            Console.WriteLine("Wpisz help w celu wyświetlenia wszystkich dostępnych komend\n");
            Task.Run(() => EstablishCloudConnection(host));
            Console.Title = host.host_name;
            Thread.Sleep(1000);
            Task.Run(() => cpcc.EstablishNCCConnection());
            while (flag)
            {
                Handler = Console.ReadLine().ToLower();
                if (Handler.Contains("sendmessage"))
                {
                    int index_of_message = 0;
                    int index_of_quanity = 0;
                    index_of_message = Handler.IndexOf("-m");
                    index_of_quanity = Handler.IndexOf("-q");
                    if (index_of_message == -1 || index_of_quanity == -1 || index_of_message == index_of_quanity)
                    {
                        if (Handler.IndexOf("sendmessage") == 0)
                        {
                            Handler = "sendmessage";
                            Parameters[0] = null;
                            Parameters[1] = null;
                        }
                    }
                    else if (index_of_message < index_of_quanity)
                    {
                        Parameters[0] = Handler.Substring(index_of_message + 2, index_of_quanity - index_of_message - 2);
                        Parameters[1] = Handler.Substring(index_of_quanity + 2);
                        Handler = Handler.Substring(0, index_of_message - 1);
                        Parameters[0] = Parameters[0].Trim();
                        Parameters[1] = Parameters[1].Trim();
                        Handler = Handler.Trim();
                    }
                    else if (index_of_message > index_of_quanity)
                    {
                        Parameters[0] = Handler.Substring(index_of_message + 2);
                        Parameters[1] = Handler.Substring(index_of_quanity + 2, index_of_message - index_of_quanity - 2);
                        Handler = Handler.Substring(0, index_of_quanity - 1);
                        String.Concat(Parameters[0].Where(c => !Char.IsWhiteSpace(c)));
                        String.Concat(Parameters[1].Where(c => !Char.IsWhiteSpace(c)));
                        String.Concat(Handler.Where(c => !Char.IsWhiteSpace(c)));
                    }

                }
                else if (Handler.Contains("deleteconnection"))
                {
                    int index = 0;
                    index = Handler.IndexOf("--");
                    if (index != -1)
                    {
                        Parameters[0] = Handler.Substring(index + 2);
                        Handler = Handler.Substring(0, index - 1);
                        Parameters[0] = Parameters[0].Trim();
                        Handler = Handler.Trim();
                    }
                    else
                    {
                        if (Handler.IndexOf("deleteconnection") == 0)
                        {
                            Handler = "deleteconnection";
                            Parameters[0] = null;
                        }
                    }
                }
                switch (Handler)
                {
                    case "help":
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine("Dostępne komendy:");
                        Console.WriteLine("sendmessage -m <Message> -q <Ilość wysłanych wiadomości>");
                        Console.WriteLine("help");
                        Console.WriteLine("clearconsole");
                        Console.WriteLine("isreceiving");
                        Console.WriteLine("changestatus");
                        Console.WriteLine("callrequest");
                        Console.WriteLine("deleteconnection -- <Adres IP docelowego Hosta>");
                        Console.WriteLine("exit");
                        Console.ResetColor();
                        break;
                    case "isreceiving":
                        Console.WriteLine(host.IsReceiving);
                        break;
                    case "changestatus":
                        Console.WriteLine($"Zmiana statusu odbierania z {host.IsReceiving} na {!host.IsReceiving}");
                        host.IsReceiving = !host.IsReceiving;
                        cpcc.updatedStatus();
                        break;
                    case "sendmessage":
                        if (Parameters != null)
                        {
                            if (int.Parse(Parameters[1]) > 0)
                            {
                                string user_choice_ = null;
                                if (call_list != null && call_list.Count != 0)
                                {
                                    Console.WriteLine("Wybierz hosta z listy połączeń, do którego chcesz wysłać wiadomość:");
                                    foreach (OtherHostsInfo i in call_list)
                                    {
                                        Console.WriteLine($"{call_list.IndexOf(i) + 1}.{i.other_host_name}:{i.IP} Call_ID: {i.call_ID} Bandwidth: {i.bandwidth} [b]");
                                    }
                                    user_choice_ = Console.ReadLine();
                                    if (user_choice_.All(Char.IsDigit) == true)
                                    {
                                        if (int.Parse(user_choice_) > 0 && int.Parse(user_choice_) <= call_list.Count)
                                        {
                                            string message = Parameters[0];
                                            int how_many = int.Parse(Parameters[1]);
                                            int index_number = int.Parse(user_choice_);
                                            Task.Run(async () =>
                                            {
                                                for (int i = 0; i < how_many; i++)
                                                {
                                                    sendMessage(host, message, call_list[index_number - 1].lambdas);

                                                    await Task.Delay(TimeSpan.FromMilliseconds(2000));
                                                }
                                            });
                                        }
                                        else
                                            Console.WriteLine("Wybrany element nie istnieje na liście");
                                    }
                                    else
                                        Console.WriteLine("Podano błędny numer na liście hostów");
                                }
                                else
                                    Console.WriteLine("Brak połączeń na liście najpierw zestaw połączenie używając callrequest");
                            }
                            else
                                Console.WriteLine("Podano błędne parametry");
                        }
                        else
                        {
                            Console.WriteLine("Błędna forma wiadomości wpisz help w celu sprawdzenia poprawnej formuły");
                        }
                        break;
                    case "deleteconnection":
                        string message_ = "Call release_req";
                        string _user_choice_ = null;

                        Console.WriteLine("Wybierz hosta z listy połączeń z którym chcesz zerwać połączenie");
                        if (call_list != null && call_list.Count !=0)
                        {
                            foreach (OtherHostsInfo i in call_list)
                            {
                                Console.WriteLine($"{call_list.IndexOf(i) + 1}.{i.other_host_name}:{i.IP} Call_ID: {i.call_ID} Bandwidth: {i.bandwidth} [b]");
                            }
                            _user_choice_ = Console.ReadLine();
                            if (_user_choice_.All(Char.IsDigit) == true)
                            {
                                if (int.Parse(_user_choice_) > 0 && int.Parse(_user_choice_) <= call_list.Count)
                                {
                                    int index_number = int.Parse(_user_choice_);
                                    cpcc.sendDeleteMessage(call_list[index_number - 1].other_host_name, message_, 0, call_list[index_number - 1].call_ID);
                                    AddLogInfo($"Przesłano prośbe o usunięcie połączenia z hostem: {call_list[index_number - 1].other_host_name} : Call release_req");
                                }
                                else
                                    Console.WriteLine("Wybrany element nie istnieje na liście");
                            }
                            else
                                Console.WriteLine("Podano błędny numer na liście hostów");
                        }
                        else
                            Console.WriteLine("Brak połączeń na liście najpierw zestaw połączenie używając callrequest");
                        break;
                    case "callrequest":
                                bool isTooHigh = false;
                                string user_choice = null;
                                //tutaj wpisujecie np. 10G co oznacza 10gb/s, albo 10M co oznacza 10megabitow itp.
                                string desired_Speed = null;
                                double desired_Speed_to_sent = 0;
                                string DestinationName = null;
                                int index_of_size = 0;
                                string temp_case = null;


                                Console.WriteLine("Wpisz nazwę hosta do którego chcesz wysłać żądanie zestawienia połączenia w formule H<nr> np. H2");
                                user_choice = Console.ReadLine();
                                Console.WriteLine("Wpisz oczekiwana przepustowość przesyłu danych w formacie:<przepustowość><miara> np. 10G:");
                                Console.WriteLine("Dostępne miary to: G,M,k");
                                desired_Speed = Console.ReadLine();
                                try
                                {
                                    DestinationName = user_choice;
                                    if (desired_Speed.Contains("G"))
                                    {
                                        temp_case = "G";
                                        index_of_size = desired_Speed.IndexOf("G");
                                    }
                                    else if (desired_Speed.Contains("M"))
                                    {
                                        temp_case = "M";
                                        index_of_size = desired_Speed.IndexOf("M");
                                    }
                                    else if (desired_Speed.Contains("k"))
                                    {
                                        temp_case = "k";
                                        index_of_size = desired_Speed.IndexOf("k");
                                    }
                                    if (temp_case != null)
                                    {
                                        switch (temp_case)
                                        {
                                            case "G":
                                                desired_Speed_to_sent = double.Parse(desired_Speed.Substring(0, index_of_size)) * 1000000000;
                                                break;
                                            case "M":
                                                desired_Speed_to_sent = double.Parse(desired_Speed.Substring(0, index_of_size)) * 1000000;
                                                break;
                                            case "k":
                                                desired_Speed_to_sent = double.Parse(desired_Speed.Substring(0, index_of_size)) * 1000;
                                                break;
                                        }
                                        if (desired_Speed_to_sent > 200000000000)
                                        {
                                            isTooHigh = true;
                                        }
                                    }
                                    else
                                    {
                                        AddErrorInfo("Podano błędną przepustowość");
                                    }
                                }
                                catch (Exception e)
                                {
                                    AddErrorInfo($"Podano błędne dane: {e.Message}");
                                }
                                if (DestinationName != null && desired_Speed_to_sent != 0 && isTooHigh == false)
                                {
                                    Task.Run(() =>
                                    {
                                            AddLogInfo($"Przesłano prośbe: Call request_req do NCC o zestawiene połączenia z hostem o nazwie: {DestinationName}, oczekiwana przepustowość: {desired_Speed_to_sent} [b]");
                                            cpcc.sendMessage(DestinationName, "Call request_req", desired_Speed_to_sent, false,0);
                                    });
                                }
                                else if (isTooHigh)
                                {
                                    AddErrorInfo("Podano za dużą przepustowość (ponad 200G)");
                                }
                                else
                                {
                                    AddErrorInfo("Podano błędne dane");
                                }
                        break;
                    case "clearconsole":
                        Console.Clear();
                        break;
                    case "exit":
                        flag = false;
                        CPCC.established_socket.Disconnect(true);
                        break;
                    default:
                        AddErrorInfo("Wpisano błędną komendę wpisz help w celu sprawdzenia dostępnych komend");
                        break;
                }
            }

        }
        public static void sendMessage(HostParseConfig host, string payload, string[] lambdas)
        {
            if(established_socket == null || !established_socket.Connected)
                return;

            ImpulseSignalPackage package = new ImpulseSignalPackage();

            package.ID = sent_packages++;
            package.Payload = payload;
            package.Port = host.output_port;
            package.Lambda_Range = lambdas;
            package.PrevIP = host.IP;

            try
            {
                established_socket.Send(package.ToBytes());
                AddLogInfo($"Wysłano sygnał : {package.Packet_Information()}");
            }
            catch(Exception e)
            {
                AddErrorInfo($"Nie udało się wysłać sygnału: {e.Message}");
            }
            
        }
        public static void listenMessage(HostParseConfig host)
        {
            while (true)
            {
                while (established_socket == null || !established_socket.Connected)
                {
                    AddLogInfo("Próba wznowienia połączenia z chmurą kablową");
                    EstablishCloudConnection(host);
                }

                try
                {
                    ImpulseSignalPackage package = established_socket.Receive();

                    if (package != null)
                    {
                        AddLogInfo($"Otrzymano sygnał: {package.Packet_Information()}");
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            AddErrorInfo("Zerwano połączenie z chmurą kablową!");
                            continue;
                        }

                        else
                        {
                            AddErrorInfo("Nie udało się połączyć z chmurą kablową!");
                        }
                    }
                }

            }
        }
        public static ImpulseSignalSocket EstablishCloudConnection(HostParseConfig host)
        {
            AddLogInfo($"Trwa łączenie z chmurą kablową {host.cloud_IP}:{host.cloud_port}");
            try
            {
                established_socket = new ImpulseSignalSocket(host.cloud_IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                established_socket.Connect(new IPEndPoint(host.cloud_IP, host.cloud_port));
                established_socket.Send(Encoding.ASCII.GetBytes($"HELLO-{host.IP}"));
                Task.Run(() => listenMessage(host));
                AddLogInfo("Zestawiono połączenie z chmurą kablową");

            }
            catch (Exception)
            {
                AddErrorInfo("Nie udało się połączyć z chmurą kablową");
            }
            return established_socket;

        }
        public static void AddConnection(string[] lambdas,IPAddress destination, string name,int ID,double bandwidth)
        {
            call_list.Add(new OtherHostsInfo(destination, name, lambdas, ID,bandwidth));
        }
        public static void DeleteConnection(string name, int ID)
        {
            OtherHostsInfo temp = new OtherHostsInfo();
            foreach(OtherHostsInfo i in call_list)
            {
                if (i.call_ID == ID && i.other_host_name == name)
                {
                    temp = i;
                    break;
                }
                else
                    temp = null;
            }
            if (temp == null)
            {
                AddLogInfo($"Nie znaleziono połączenia do usunięcia do hosta: {name} o call_ID: {ID}");
            }
            else
            {
                call_list.Remove(temp);
                Host.AddLogInfo($"Usunięcie połączenia o ID: {ID} z {name} powiodło się");
            }
        }
        public static void AddLogInfo(string info)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {info}");
            Console.ResetColor();
        }
        public static void AddErrorInfo(string info)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {info}");
            Console.ResetColor();
        }

    }
}
