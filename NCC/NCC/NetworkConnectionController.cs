using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commons.Tools;

namespace NCC
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

    // Server that stands for the NCC
    class NCC
    {
        //This field controls a thread
        private ManualResetEvent allDone;
        private CC_Agent cc_Agent;
        //Address, port and endPoint
        private IPAddress address;
        private IPEndPoint localEndPoint;
        private Dictionary<string, IPAddress> Dict;
        private int port;
        private List<string> lost_hosts = new List<string>();
        private Socket temporarySource = null;

        //Dictionary that stores connected sockets and thier names
        Dictionary<string, Socket> ClientSockets;

        //Message package
        private MessagePackage messagePackage;


        public NCC()
        {
            allDone = new ManualResetEvent(false);
            ClientSockets = new Dictionary<string, Socket>();
            messagePackage = new MessagePackage();
            Dict = new Dictionary<string, IPAddress>();
        }


        //Start the server
        public void StartNCC(NCCConfig config)
        {
            cc_Agent = new CC_Agent(config);
            Task.Run(()=>cc_Agent.EstablishCCConnection());
            address = IPAddress.Parse(config.IP);
            port = int.Parse(config.Port);
            localEndPoint = new IPEndPoint(address, port);
            Dict = config.Directory;
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
            bool disconnected = false;
            try
            {
                bytesRead = handler.EndReceive(asyncResult);
                if (bytesRead == 0)
                {
                    var cpcc = ClientSockets.First(x => x.Value == handler);
                    AddLog($"Otrzymano informacje od jednego z CPCC: Host {cpcc.Key} został wyłączony", ConsoleColor.Red);
                    Task.Run(() => getResponse("http://127.0.0.1:8050/api/breakdown/"+cpcc.Key));
                    lost_hosts.Add(cpcc.Key);
                    ClientSockets.Remove(cpcc.Key);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    disconnected = true;
                }
            }
            catch
            {
                return;
            }

            dataReader.receivedData.Clear();

            if (bytesRead > 0)
            {
                byte[] bytesReceived = new byte[bytesRead];
                Array.Copy(dataReader.buffer, bytesReceived, bytesRead);
                Commute(handler, dataReader, bytesReceived);
            }
            if (disconnected == false)
            {
                handler.BeginReceive(dataReader.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReadCallback), dataReader);
            }
        }
        static async Task getResponse(string sitehttp)
        {
            HttpClient client = new HttpClient();
            try
            {
                string responseBody = await client.GetStringAsync(sitehttp);
            }
            catch
            {
                    
            }
        }
        public void Send(Socket handler, StateObject st, MessagePackage data)
        {
            byte[] byteData = null;
            switch (data.Payload)
            {
                case "Call request_req":
                    AddLog($"Wysyłanie zapytania do docelowego CPCC: {data.DestinationName} czy przyjmuje połączenia: Call Accept_req",ConsoleColor.Green);
                    data.Payload = "Call Accept_req";
                    data.DestinationIP = null;
                    data.SourceIP = null;
                    //Convert to bits
                    byteData = data.ToBytes();
                    //Begin sending data to client
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    break;
                case "Call Accept_rsp(True)":
                    //AddLog($"Wybrany host przyjmuje połączenia, wysyłanie informacji do CPCC przy hoście: {data.DestinationName}", ConsoleColor.Green);
                    data.Payload = "Call pending";
                    //Convert to bits
                    byteData = data.ToBytes();
                    //Begin sending data to client
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    break;
                case "Call Accept_rsp(False)":
                    AddLog($"Wybrany host nie przyjmuje połączeń, wysyłanie informacji: Call request_rsp(refused) do CPCC przy hoście: {data.DestinationName}", ConsoleColor.Green);
                    data.Payload = "Call request_rsp(refused)";
                    data.DestinationIP = null;
                    data.SourceIP = null;
                    //Convert to bits
                    byteData = data.ToBytes();
                    //Begin sending data to client
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    break;
                case "Connection request":
                    AddLog("Wysyłanie prośby o zestawienie połączenia Connection request_req do CC",ConsoleColor.Green);
                    if (CC_Agent.established_socket == null || !CC_Agent.established_socket.Connected)
                    {
                        AddLog("Brak połączenia z CC, wysyłanie CPCC przy hoście źródłowym informacji Call request_rsp(closed)",ConsoleColor.Green);
                        data.Payload = "Call request_rsp(closed)";
                        data.power_level = 0;
                        data.desired_bandwidth = 0;
                        Socket s;
                        s = ClientSockets.First(x => x.Key == data.SourceIP.ToString()).Value;
                        data.DestinationIP = null;
                        data.SourceIP = null;
                        byteData = data.ToBytes();
                        s.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), s);
                    }
                    else
                    {
                        data.Payload = "Connection request_req";
                        //Begin sending message to CC
                        cc_Agent.sendMessage(data.SourceIP, data.DestinationIP, data.Payload, data.desired_bandwidth, data.power_level,data.call_ID);
                        Task.Run(() => waitForResponse(cc_Agent, st));
                    }
                    break;
                case "Call release_req":
                    AddLog("Wysyłanie prośby o usunięcie połączenia Connection request_req(release) do CC", ConsoleColor.Green);
                    if (CC_Agent.established_socket == null || !CC_Agent.established_socket.Connected)
                    {
                        AddLog("Brak połączenia z CC, wysyłanie CPCC przy hoście źródłowym informacji Call release_rsp(closed)", ConsoleColor.Green);
                        data.Payload = "Call release_rsp(closed)";
                        data.power_level = 0;
                        data.desired_bandwidth = 0;
                        Socket s;
                        s = ClientSockets.First(x => x.Key == data.SourceIP.ToString()).Value;
                        data.DestinationIP = null;
                        data.SourceIP = null;
                        byteData = data.ToBytes();
                        s.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), s);
                    }
                    else
                    {
                        data.Payload = "Connection request_req(release)";
                        //Begin sending message to CC
                        cc_Agent.sendMessage(data.SourceIP, data.DestinationIP, data.Payload, data.desired_bandwidth, data.power_level,data.call_ID);
                        Task.Run(() => waitForResponse(cc_Agent, st));
                    }
                    break;
            }
        }
        public void waitForResponse(CC_Agent cc,StateObject st)
        {
            bool flag = true;
            if (CC_Agent.established_socket == null || !CC_Agent.established_socket.Connected)
            {
                flag = false;
            }
            while (flag)
            {
                if (cc.messageToNCC != null)
                {
                    try
                    {
                        Socket s;
                        s = ClientSockets.First(x => x.Key == cc.messageToNCC.SourceIP.ToString()).Value;
                        string[] message = cc.messageToNCC.Payload.Split("@");
                        if (message[0]=="Connection request_rsp")
                        {
                            message[0] = "Call request_rsp";
                        }
                        else if(message[0]=="Connection request_rsp(release)")
                        {
                            message[0] = "Call release_rsp";
                            foreach(IPAddress m in Dict.Values)
                            {
                                if (m.ToString() == cc.messageToNCC.SourceIP.ToString())
                                {
                                    cc.messageToNCC.SourceName = Dict.FirstOrDefault(x => x.Value == m).Key;
                                }
                                else if(m.ToString() == cc.messageToNCC.DestinationIP.ToString())
                                {
                                    cc.messageToNCC.DestinationName = Dict.FirstOrDefault(x => x.Value == m).Key;
                                }
                            }
                            cc.messageToNCC.DestinationIP = null;
                            cc.messageToNCC.SourceIP = null;
                        }
                        else if (message[0] == "Connection request_rsp(release false)")
                        {
                            message[0] = "Call release_rsp(false)";
                            foreach (IPAddress m in Dict.Values)
                            {
                                if (m.ToString() == cc.messageToNCC.SourceIP.ToString())
                                {
                                    cc.messageToNCC.SourceName = Dict.FirstOrDefault(x => x.Value == m).Key;
                                }
                                else if (m.ToString() == cc.messageToNCC.DestinationIP.ToString())
                                {
                                    cc.messageToNCC.DestinationName = Dict.FirstOrDefault(x => x.Value == m).Key;
                                }
                            }
                            cc.messageToNCC.DestinationIP = null;
                            cc.messageToNCC.SourceIP = null;
                        }
                        if (message.Length == 2)
                        {
                            cc.messageToNCC.Payload = message[0] + "@" + message[1];
                        }
                        else if (message.Length == 1)
                        {
                            cc.messageToNCC.Payload = message[0];
                        }
                        byte[] byteData = cc.messageToNCC.ToBytes();
                        if (!cc.messageToNCC.Payload.Contains("release"))
                        {
                            foreach (IPAddress m in Dict.Values)
                            {
                                if (m.ToString() == cc.messageToNCC.SourceIP.ToString())
                                {
                                    cc.messageToNCC.SourceName = Dict.FirstOrDefault(x => x.Value == m).Key;
                                }
                            }
                            AddLog($"Wysyłanie odpowiedzi: {cc.messageToNCC.Payload} do CPCC przy hoście: {cc.messageToNCC.SourceName}", ConsoleColor.Green);
                            cc.messageToNCC.SourceName = null;
                        }
                        else
                        {
                            AddLog($"Wysyłanie odpowiedzi: {cc.messageToNCC.Payload} do CPCC przy hoście: {cc.messageToNCC.SourceName}", ConsoleColor.Green);
                        }
                        s.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), s);
                        cc.clearNCC();
                        flag = false;
                    }
                    catch
                    {

                    }
                }
            }

        }
        public void SendCallback(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                handler = (Socket)ar.AsyncState;
                //Complete sending
                int bytesSend = handler.EndSend(ar);
                //AddLog($"Wyslano informacje do docelowego CPCC.", ConsoleColor.Green);
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

        //Handling the packet
        public void Commute(Socket socket, StateObject state, byte[] mess)
        {
            
            MessagePackage packet = null;
            try
            {
                packet = MessagePackage.FromBytes(mess);
                if (packet.Payload != "Connection request")
                {
                    AddLog($"Otrzymano informacje: {packet.Packet_Information()}", ConsoleColor.White);
                }
                if (packet != null)
                {
                    checkIfReceive(socket, state, packet);
                }
            }
            catch
            {
                string message = Encoding.ASCII.GetString(mess).Trim();
                string[] parts = message.Split("-");
                if (parts[0].Equals("HELLO"))
                {
                    if (lost_hosts.Contains(parts[1]))
                    {
                        Task.Run(() => getResponse("http://127.0.0.1:8050/api/repair/" + parts[1]));
                    }
                    SaveSocket(socket, parts[1]);
                    AddLog(message, ConsoleColor.Cyan);
                }
                else if(packet!=null)
                {
                    if (packet.Payload.Contains("request_req"))
                    {
                        packet.Payload = "Call request_rsp(refused)";
                    }
                    else if (packet.Payload.Contains("release_req"))
                    {
                        packet.Payload = "Call release_rsp(refused)";
                    }
                    AddLog($"Wybrany host nie istnieje, wysyłanie informacji {packet.Payload} do CPCC przy hoście: {packet.SourceName}", ConsoleColor.Green);
                    packet.desired_bandwidth = 0;
                    packet.power_level = 0;
                    Socket s;
                    s = ClientSockets.First(x => x.Key == packet.SourceIP.ToString()).Value;
                    packet.DestinationIP = null;
                    packet.SourceIP = null;
                    byte[] byteData = packet.ToBytes();
                    //Begin sending data to client
                    s.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), s);
                }
            }

        }
        public void checkIfReceive(Socket s, StateObject state, MessagePackage data)
        {
            temporarySource = s;
            bool isValid = true;
            if (data.Payload == "Call request_req")
            {
                if (data.SourceIP == null)
                {
                    AddLog($"Wysłano żądanie do Directory: Directory request_req=>Nazwa hosta: {data.SourceName}", ConsoleColor.Green);
                    AddLog($"Directory: Otrzymano żądanie: Directory request_req=>Nazwa hosta: {data.SourceName}", ConsoleColor.DarkYellow);
                    try
                    {
                        data.SourceIP = Dict.First(x => x.Key == data.SourceName).Value;
                        AddLog($"Directory: Wysyłanie odpowiedzi: Directory request_rsp=>Adres IP interfejsu hosta: {data.SourceIP}", ConsoleColor.DarkYellow);
                        AddLog($"Otrzymano odpowiedź od Directory: Directory request_rsp=>Adres IP interfejsu hosta: {data.SourceIP}", ConsoleColor.Green);
                    }
                    catch
                    {
                        AddLog($"Directory: Wysyłanie odpowiedzi: Directory request_rsp=>Adres IP interfejsu hosta: OutOfDirectory", ConsoleColor.DarkYellow);
                        AddLog($"Otrzymano odpowiedź od Directory: Directory request_rsp=>Adres IP interfejsu hosta: OutOfDirectory", ConsoleColor.Green);
                        isValid = false;
                    }

                }
                if (data.DestinationIP == null)
                {
                    AddLog($"Wysłano żądanie do Directory: Directory request_req=>Nazwa hosta: {data.DestinationName}", ConsoleColor.Green);
                    AddLog($"Directory: Otrzymano żądanie: Directory request_req=>Nazwa hosta: {data.DestinationName}", ConsoleColor.DarkYellow);
                    try
                    {
                        data.DestinationIP = Dict.First(x => x.Key == data.DestinationName).Value;
                        AddLog($"Directory: Wysyłanie odpowiedzi: Directory request_rsp=>Adres IP interfejsu hosta: {data.DestinationIP}", ConsoleColor.DarkYellow);
                        AddLog($"Otrzymano odpowiedź od Directory: Directory request_rsp=>Adres IP interfejsu hosta: {data.DestinationIP}", ConsoleColor.Green);
                    }
                    catch
                    {
                        AddLog($"Directory: Wysyłanie odpowiedzi:Directory request_rsp=>Adres IP interfejsu hosta: OutOfDirectory", ConsoleColor.DarkYellow);
                        AddLog($"Otrzymano odpowiedź od Directory: Directory request_rsp=>Adres IP interfejsu hosta: OutOfDirectory", ConsoleColor.Green);
                        isValid = false;
                    }

                }
                if (isValid)
                {
                    AddLog($"Wysłano żądanie do Policy: Policy request_req=>Adres IP hosta źródłowego: {data.SourceIP} Nazwa: {data.SourceName} Adres IP hosta docelowego: {data.DestinationIP} Nazwa: {data.DestinationName}, Żądana przepustowość: {data.desired_bandwidth}", ConsoleColor.Green);
                    AddLog($"Policy: Otrzymano żądanie:Policy request_req=>Adres IP hosta źródłowego: {data.SourceIP} Nazwa: {data.SourceName} Adres IP hosta docelowego: {data.DestinationIP} Nazwa: {data.DestinationName}, Żądana przepustowość: {data.desired_bandwidth}", ConsoleColor.DarkMagenta);
                    AddLog($"Policy: Żądanie przeszło pomyślnie autoryzację wysyłanie odpowiedzi: Policy request_rsp Accepted", ConsoleColor.DarkMagenta);
                    AddLog($"Otrzymano odpowiedź od Policy: Policy request_rsp Accepted=>Adres IP hosta źródłowego: {data.SourceIP} Nazwa: {data.SourceName} Adres IP hosta docelowego: {data.DestinationIP} Nazwa: {data.DestinationName}, Żądana przepustowość: {data.desired_bandwidth}", ConsoleColor.Green);
                }
                data.desired_bandwidth = 0;
            }
            if(data.SourceIP == null)
            {
                data.SourceIP = Dict.First(x => x.Key == data.SourceName).Value;
            }
            if(data.DestinationIP == null)
            {
                data.DestinationIP = Dict.First(x => x.Key == data.DestinationName).Value;
            }
            s = ClientSockets.First(x => x.Key == data.DestinationIP.ToString()).Value;
            Send(s, state, data);
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
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]: {s}");
            else
            {
                MessagePackage package = (MessagePackage)s;
                Console.WriteLine(package.Packet_Information());
            }
            Console.ResetColor();
        }
    }
}

