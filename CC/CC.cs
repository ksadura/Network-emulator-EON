using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commons.Tools;

namespace CC
{
    class StateObject
    {
        public static readonly int BufferSize = 1024;
        public byte[] buffer;
        public StringBuilder receivedData = new StringBuilder();
        public Socket tempSocket = null;

        public StateObject() => buffer = new byte[BufferSize];
    }

    class CC
    {
        private ManualResetEvent allDone;
        private static RC_Agent rc_Agent;
        private LRM_Agent lrm_Agent;
        private IPAddress address;
        private IPEndPoint localEndPoint;
        private ushort port;
        public static bool isSubCC;
        private List<RouteTableRow> subNetworks;
        private Dictionary<ushort, IPAddress> subPortNodes;
        private string[] subLambdaRange;
        private int subLambdaAmt;
        private IPAddress subSource;
        private IPAddress subDestination;
        private ushort subPortIn;
        private ushort subPortOut;

        private static Dictionary<string, Socket> clientSockets;
        public static Dictionary<MessagePackage, RouteTableQuery> storedRoutes;
        private string[] lambda_range;
        private int lambda_amt;
        private static bool reconfigured = false;

        private MessagePackage messagePackage;
        private Dictionary<Tuple<IPAddress, IPAddress>, string> Modulations;
        private Dictionary<string, string[]> subCCRoutes;
        private static List<string> errorRoute;
        private static List<string> fixedRoute;

        private ManualResetEvent _receivedDone = new ManualResetEvent(false);
        private ManualResetEvent mre = new ManualResetEvent(false);
        private ManualResetEvent mre1 = new ManualResetEvent(false);
        private Socket socketToMainCC;

        public CC()
        {
            allDone = new ManualResetEvent(false);
            clientSockets = new Dictionary<string, Socket>();
            storedRoutes = new Dictionary<MessagePackage, RouteTableQuery>();
            messagePackage = new MessagePackage();
            lambda_range = null;
            lambda_amt = 0;
            subNetworks = new List<RouteTableRow>();
            subCCRoutes = new Dictionary<string, string[]>();
            errorRoute = new List<string>();
            fixedRoute = new List<string>();
        }


        //Start the server
        public void StartCC(Config config)
        {
            Modulations = config.Modulations;
            rc_Agent = new RC_Agent(config);
            Task.Run(() => RC_Agent.EstablishRCConnection());
            lrm_Agent = new LRM_Agent(config);
            Task.Run(() => lrm_Agent.EstablishLRMConnection());
            address = config.IP;
            port = config.Port;
            localEndPoint = new IPEndPoint(address, port);
            Socket listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            isSubCC = config.isSubCC;
            subPortNodes = config.PortNodes;

            socketToMainCC = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (config.isSubCC)
            {
                ConnectToCC(config, socketToMainCC);
            }

            try
            {

                listener.Bind(localEndPoint);
                listener.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    AddLog("Waiting for a connection...", LogType.Information);
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();

                    if (config.isSubCC)
                    {
                        _receivedDone.Reset();
                        Receive(socketToMainCC);
                        _receivedDone.WaitOne();
                    }
                }
            }
            catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace, LogType.Error);
            }
        }

        public void Receive(Socket socket)
        {
            try
            {
                StateObject state = new StateObject();
                state.tempSocket = socket;
                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = ar.AsyncState as StateObject;
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
            Commute(handler, state, array);
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
            _receivedDone.Set();

        }

        private void AcceptCallback(IAsyncResult asyncResult)
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

        private void ReadCallback(IAsyncResult asyncResult)
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
                AddLog(e.Message + e.StackTrace, LogType.Error);
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
            mre.Set();
            mre1.Set();
        }

        private void Commute(Socket socket, StateObject state, byte[] mess)
        {
            try
            {
                var networkObject = clientSockets.First(x => x.Value == socket);
                switch (networkObject.Key)
                {
                    case "NCC":
                        CommuteNCCPacket(socket, state, mess);
                        break;
                    case string x when x.StartsWith("R"):
                        string response = Encoding.ASCII.GetString(mess).Trim();
                        switch (response)
                        {
                            case "OK":
                                if (!isSubCC)
                                {
                                    AddLog($"Received Connection request_rsp from node {networkObject.Key}", LogType.Information);
                                }
                                break;
                            case "OK_DELETED":
                                if (!isSubCC)
                                {
                                    AddLog($"Received Connection_request_rsp(release) from node {networkObject.Key}", LogType.Information);
                                }
                                break;
                            default:
                                AddLog($"Node {networkObject.Key} operation failed", LogType.Error);
                                break;
                        }
                        break;
                    case "CC":
                        CommuteCCPacket(socket, state, mess);
                        break;
                    default:
                        string res = Encoding.ASCII.GetString(mess.ToArray());
                        if (res.Split("-").Length > 0)
                        {
                            if (res.Split("-")[0] == "OK")
                            {
                                handlesubCCResponse(res);
                                AddLog($"Received Connection request_rsp from subnetwork {res.Split("-")[1]}", LogType.Information);
                            } else if (res.Split("-")[0] == "OK_DEL")
                            {
                                handlesubCCResponse(res);
                                AddLog($"Received Connection request_rsp(release) from subnetwork {res.Split("-")[1]}", LogType.Information);
                            }
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                string message = Encoding.ASCII.GetString(mess).Trim();
                string[] parts = message.Split("-");
                if (parts[0].Equals("HELLO"))
                {
                    SaveSocket(socket, parts[1]);
                    AddLog(message, LogType.Add);
                }
                else
                {
                    AddLog(e.Message + e.StackTrace, LogType.Error);
                }
            }
        }

        public static void reconfigure(string res)
        {
            if (res.Split("-").Length > 0)
            {
                if (res.Split("-")[0] == "ERROR")
                {
                    AddLog("Received Link connection request_rsp(error) from LRM", LogType.Information);
                    errorRoute.Clear();
                    if (storedRoutes.Count > 0)
                    {
                        string[] errorNodes = res.Split("-")[1].Split("&");
                        foreach (var route in storedRoutes.Values)
                        {
                            List<string> nodes = getNodesFromRoute(route);
                            if (nodes.Contains(errorNodes[0]) && nodes.Contains(errorNodes[1]))
                            {
                                errorRoute = nodes;
                                AddLog("Route found. Starting reconfigure...", LogType.Information);
                                StateObject state = new StateObject();
                                MessagePackage messagePackage = storedRoutes.FirstOrDefault((x) => x.Value == route).Key;
                                reconfigureRoute(route, state, messagePackage.desired_bandwidth);
                                string apiRequest = changeAPIRequest();
                                Task.Run(() => getResponseFromAPI(apiRequest));
                            }
                        }

                    }
                    else
                    {
                        AddLog("No stored routes. Error will not affect.", LogType.Information);
                    }
                }

            }
        }

        private static List<string> getNodesFromRoute(RouteTableQuery route)
        {
            List<string> tmp = new List<string>();
            foreach (var row in route.RouteTableRows)
            {
                tmp.Add(row.NodeName);
            }
            return tmp;
        }

        private static void reconfigureRoute(RouteTableQuery routeTableQuery, StateObject state, double bandwith)
        {
            string[] lambda_range = routeTableQuery.RouteTableRows[0].Lambda_Range;
            int lambda_amt = routeTableQuery.Lambda_Amount;
            routeTableQuery.RouteTableRows.Clear();
            routeTableQuery.bandwith = bandwith;
            RC_Agent.sendMessage(routeTableQuery);
            var t = Task.Run(() => waitForNewRoute(rc_Agent, state, lambda_range));
            t.Wait();
            AddLog("Route reconfigure success", LogType.Information);
            fixedRoute = getNodesFromRoute(RC_Agent.ROUTE_TABLE_QUERY);
            string releaseRequest = prepareReconfigureReleaseRequest(lambda_range);
            LRM_Agent.sendReleaseMessage(releaseRequest);
            sendToLRM(lambda_amt, fixedRoute, lambda_range);
            rc_Agent.clearPath();
            reconfigured = true;
        }

        private static string prepareReconfigureReleaseRequest(string[] lambda_range)
        {
            StringBuilder sb = new StringBuilder("RELEASE-");
            sb.Append(lambda_range[0]);
            sb.Append("&");
            sb.Append(lambda_range[1]);
            sb.Append("-");
            foreach (var row in errorRoute)
            {
                sb.Append(row);
                sb.Append("&");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        private static void waitForNewRoute(RC_Agent rC_Agent, StateObject st, string[] lambda_range)
        {
            bool flag = true;
            if (RC_Agent.established_socket == null || !RC_Agent.established_socket.Connected || LRM_Agent.established_socket == null || !LRM_Agent.established_socket.Connected)
            {
                flag = false;
            }
            while (flag)
            {
                if (RC_Agent.ROUTE_TABLE_QUERY != null)
                {
                    try
                    {
                        foreach (RouteTableRow routeTableRow in RC_Agent.ROUTE_TABLE_QUERY.RouteTableRows)
                        {
                            try
                            {
                                var networkObject = clientSockets.First(x => x.Key == routeTableRow.NodeName);
                                Socket nodeSocket = networkObject.Value;
                                routeTableRow.action = "ADD";
                                routeTableRow.Lambda_Range = lambda_range;
                                Send(nodeSocket, st, routeTableRow, false);
                            }
                            catch (ArgumentNullException null_e)
                            {
                                AddLog($"Connection with Node in route from RC not established, {null_e.Message}", LogType.Error);
                            }
                        }
                        flag = false;
                    }
                    catch (Exception e)
                    {
                        AddLog(e.Message + e.StackTrace, LogType.Error);
                    }
                }
            }
        }

        private static string changeAPIRequest()
        {
            if (errorRoute.Count>0 && fixedRoute.Count > 0)
            {
                StringBuilder sb = new StringBuilder("http://127.0.0.1:8050/api/");
                sb.Append("change").Append("/");
                foreach (var node in errorRoute)
                {
                    sb.Append(node.Replace('R','r')).Append("$");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("=");
                foreach (var node in fixedRoute)
                {
                    sb.Append(node.Replace('R', 'r')).Append("$");
                }
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
            return null;
        }

        private void CommuteNCCPacket(Socket socket, StateObject state, byte[] mess)
        {
            MessagePackage packet = null;
            try
            {
                packet = MessagePackage.FromBytes(mess);
                if (packet != null)
                {
                    messagePackage = packet;

                    switch (messagePackage.Payload)
                    {
                        case "Connection request_req":
                            AddLog($"Handle {messagePackage.Payload} from NCC", LogType.Information);
                            HandleConnectionRequest(socket, state);
                            break;
                        case "Connection request_req(release)":
                            AddLog($"Handle {messagePackage.Payload} from NCC", LogType.Information);
                            HandleCallTeardown(socket, state);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace, LogType.Error);
            }
        }

        private void CommuteCCPacket(Socket socket, StateObject state, byte[] mess)
        {
            MessagePackage packet = null;
            try
            {
                packet = MessagePackage.FromBytes(mess);
                if (packet != null)
                {
                    messagePackage = packet;
                    string payload = handlePayload(messagePackage.Payload);
                    switch (payload)
                    {
                        case "Connection request":
                            AddLog($"Handle {messagePackage.Payload} from CC", LogType.Information);
                            HandleConnectionRequest(socket, state);
                            break;
                        case "Connection request_req(release)":
                            AddLog($"Handle {messagePackage.Payload} from CC", LogType.Information);
                            HandleCallTeardown(socket, state);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace, LogType.Error);
            }
        }

        private void HandleConnectionRequest(Socket socket, StateObject state)
        {
            try
            {
                if (isSubCC)
                {
                    if (RC_Agent.established_socket == null || !RC_Agent.established_socket.Connected)
                    {
                        AddLog("Connection with RC not established", LogType.Error);
                    }
                    else
                    {
                        RouteTableQuery request = new RouteTableQuery();
                        request.SourceIP = subSource;
                        request.DestinationIP = subDestination;
                        request.bandwith = messagePackage.desired_bandwidth;
                        request.subPortIn = subPortIn;
                        request.subPortOut = subPortOut;
                        RC_Agent.sendMessage(request);
                        var t = Task.Run(() => waitForResponse(rc_Agent, state));
                        t.Wait();
                        sendMessageToCC(null);
                        AddLog("Route configured. Sending Connection request_rsp to CC", LogType.Information);
                        storedRoutes.Add(messagePackage, RC_Agent.ROUTE_TABLE_QUERY);
                        rc_Agent.clearPath();
                        lrm_Agent.clearLambda();
                        lambda_range = null;
                        lambda_amt = 0;
                        subCCRoutes.Clear();
                    }
                }
                else
                {
                    if (checkIfStoredPackage(messagePackage))
                    {
                        messagePackage.Payload = prepareNCCRequest(true);
                        byte[] byteData = messagePackage.ToBytes();
                        socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socket);
                        AddLog("This route was stored. Sending Connection request_rsp to NCC", LogType.Information);
                    }
                    else
                    {
                        if (RC_Agent.established_socket == null || !RC_Agent.established_socket.Connected)
                        {
                            AddLog("Connection with RC not established", LogType.Error);
                        }
                        else
                        {
                            RouteTableQuery request = new RouteTableQuery();
                            request.SourceIP = messagePackage.SourceIP;
                            request.DestinationIP = messagePackage.DestinationIP;
                            request.bandwith = messagePackage.desired_bandwidth;
                            RC_Agent.sendMessage(request);
                            var t = Task.Run(() => waitForResponse(rc_Agent, state));
                            t.Wait();
                            sendToSubCC();
                            messagePackage.Payload = prepareNCCRequest(false);
                            byte[] byteData = messagePackage.ToBytes();
                            socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socket);
                            AddLog($"Route configured. Sending Connection request_rsp to NCC", LogType.Information);
                            string apiRequest = prepareAPIRequest("add", null);
                            Task.Run(() => getResponseFromAPI(apiRequest));
                            RC_Agent.ROUTE_TABLE_QUERY.RouteTableRows[0].Lambda_Range = lambda_range;
                            storedRoutes.Add(messagePackage, RC_Agent.ROUTE_TABLE_QUERY);
                            rc_Agent.clearPath();
                            lrm_Agent.clearLambda();
                            lambda_range = null;
                            lambda_amt = 0;
                            subNetworks.Clear();
                            subCCRoutes.Clear();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace, LogType.Error);
            }
        }

        private void HandleCallTeardown(Socket socket, StateObject state)
        {
            if (storedRoutes.Count > 0)
            {
                foreach (MessagePackage m in storedRoutes.Keys)
                {
                    if (messagePackage.SourceIP.Equals(m.SourceIP) && messagePackage.DestinationIP.Equals(m.DestinationIP))
                    {
                        if (isSubCC)
                        {
                            RouteTableQuery routeTableQuery = null;
                            storedRoutes.TryGetValue(m, out routeTableQuery);
                            var t = Task.Run(() => deleteRouteFromNodes(routeTableQuery, state));
                            t.Wait();
                            string releaseRequest = prepareReleaseRequest(routeTableQuery);
                            if (releaseRequest != null)
                            {
                                LRM_Agent.sendReleaseMessage(releaseRequest);
                            }
                            else
                            {
                                AddLog("Unable to send release request to LRM. Error while preparing request", LogType.Error);
                            }
                            messagePackage.Payload = "Connection request_rsp(release)";
                            sendMessageToCC(routeTableQuery);
                            AddLog("Route deleted from cache. Sending Connection request_rsp(release) back to CC", LogType.Remove);
                            storedRoutes.Remove(m);
                        }
                        else
                        {
                            RouteTableQuery routeTableQuery = null;
                            storedRoutes.TryGetValue(m, out routeTableQuery);
                            var t = Task.Run(() => deleteRouteFromNodes(routeTableQuery, state));
                            t.Wait();
                            deleteRouteFromsubCC(m);
                            string releaseRequest = prepareReleaseRequest(routeTableQuery);
                            if (releaseRequest != null)
                            {
                                LRM_Agent.sendReleaseMessage(releaseRequest);
                            }
                            else
                            {
                                AddLog("Unable to send release request to LRM. Error while preparing request", LogType.Error);
                            }
                            messagePackage.Payload = "Connection request_rsp(release)";
                            byte[] byteData = messagePackage.ToBytes();
                            socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socket);
                            AddLog($"Route deleted from cache. Sending Connection request_rsp(release) to NCC", LogType.Remove);
                            string apiRequest = prepareAPIRequest("del", routeTableQuery);
                            Task.Run(() => getResponseFromAPI(apiRequest));
                            storedRoutes.Remove(m);
                            subNetworks.Clear();
                            subCCRoutes.Clear();
                        }
                        return;
                    }
                }
                AddLog("Nothing to delete. Sending Connection request_rsp(release false) to NCC", LogType.Information);
                messagePackage.Payload = "Connection request_rsp(release false)";
                byte[] bytes = messagePackage.ToBytes();
                socket.Send(bytes);
            }
            else
            {
                AddLog("No stored routes. Sending Connection request_rsp(release false) to NCC", LogType.Information);
                messagePackage.Payload = "Connection request_rsp(release false)";
                byte[] bytes = messagePackage.ToBytes();
                socket.Send(bytes);
            }
        }

        private void deleteRouteFromNodes(RouteTableQuery routeTableQuery, StateObject st)
        {
            foreach (RouteTableRow routeTableRow in routeTableQuery.RouteTableRows)
            {
                if (routeTableRow.NodeName.Substring(0, 2) == "CC")
                {
                    subNetworks.Add(routeTableRow);
                }
                else
                {
                    try
                    {
                        var networkObject = clientSockets.First(x => x.Key == routeTableRow.NodeName);
                        Socket nodeSocket = networkObject.Value;
                        routeTableRow.action = "DEL";
                        Send(nodeSocket, st, routeTableRow, true);
                    }
                    catch (ArgumentNullException null_e)
                    {
                        AddLog($"Connection with Node in route from RC not established, {null_e.Message}", LogType.Error);
                    }
                }
            }
        }

        public void waitForResponse(RC_Agent rC_Agent, StateObject st)
        {
            bool flag = true;
            if (RC_Agent.established_socket == null || !RC_Agent.established_socket.Connected || LRM_Agent.established_socket == null || !LRM_Agent.established_socket.Connected)
            {
                flag = false;
            }
            while (flag)
            {
                if (RC_Agent.ROUTE_TABLE_QUERY != null)
                {
                    try
                    {
                        if (isSubCC)
                        {
                            sendToLRM(subLambdaAmt, RC_Agent.ROUTE_TABLE_QUERY.getRouteNodes(), subLambdaRange);
                            foreach (RouteTableRow routeTableRow in RC_Agent.ROUTE_TABLE_QUERY.RouteTableRows)
                            {
                                try
                                {
                                    var networkObject = clientSockets.First(x => x.Key == routeTableRow.NodeName);
                                    Socket nodeSocket = networkObject.Value;
                                    routeTableRow.action = "ADD";
                                    routeTableRow.Lambda_Range = subLambdaRange;
                                    Send(nodeSocket, st, routeTableRow, false);
                                }
                                catch (ArgumentNullException null_e)
                                {
                                    AddLog($"Connection with Node in route from RC not established, {null_e.Message}", LogType.Error);
                                }

                            }
                        }
                        else
                        {    
                            lambda_amt = RC_Agent.ROUTE_TABLE_QUERY.Lambda_Amount;
                            lambda_range = getLambdaRangeFromLRM(lambda_amt, RC_Agent.ROUTE_TABLE_QUERY.getRouteNodes());
                            foreach (RouteTableRow routeTableRow in RC_Agent.ROUTE_TABLE_QUERY.RouteTableRows)
                            {
                                if (routeTableRow.NodeName.Substring(0, 2) == "CC")
                                {
                                    subNetworks.Add(routeTableRow);
                                }
                                else
                                {
                                    try
                                    {
                                        var networkObject = clientSockets.First(x => x.Key == routeTableRow.NodeName);
                                        Socket nodeSocket = networkObject.Value;
                                        routeTableRow.action = "ADD";
                                        routeTableRow.Lambda_Range = lambda_range;
                                        Send(nodeSocket, st, routeTableRow, false);
                                    }
                                    catch (ArgumentNullException null_e)
                                    {
                                        AddLog($"Connection with Node in route from RC not established, {null_e.Message}", LogType.Error);
                                    }
                                }
                            }
                        }
                        flag = false;
                    }
                    catch (Exception e)
                    {
                        AddLog(e.Message + e.StackTrace, LogType.Error);
                    }
                }
            }
        }

        private int CalculateLambdaAmt()
        {
            double bandwith = messagePackage.desired_bandwidth;
            // przejście na GHz
            bandwith /= 1000000000;
            // efektywność widmowa 2Hz/Baud
            bandwith *= 2;
            // modulacja
            string modulation = PickModulation();
            int modulatinIndex = int.Parse(modulation.Substring(0, 2));
            bandwith /= Math.Log2(modulatinIndex);
            // pasmo ochronne + 5 GHz z każdej strony
            bandwith += 10;
            // liczymy szczeliny 12,5 GHz
            AddLog($"Calculated slots' number: {(int)Math.Ceiling(bandwith / 12.5)}", LogType.Information);
            return (int)Math.Ceiling(bandwith / 12.5);
        }

        private string PickModulation()
        {
            string modulation = null;
            foreach (Tuple<IPAddress, IPAddress> hosts in Modulations.Keys)
            {
                if ((messagePackage.SourceIP.Equals(hosts.Item1) && messagePackage.DestinationIP.Equals(hosts.Item2)) || (messagePackage.SourceIP.Equals(hosts.Item2) && messagePackage.DestinationIP.Equals(hosts.Item1)))
                {

                    Modulations.TryGetValue(hosts, out modulation);
                }
            }
            AddLog($"Picked modulation: {modulation}", LogType.Information);
            return modulation;

        }

        private string[] getLambdaRangeFromLRM(int lambda_amt, List<string> nodes)
        {
            string request = prepareLCRequest(lambda_amt, nodes, null);
            LRM_Agent.sendMessage(request);
            while (lrm_Agent.lambda_range == null)
            {
            }
            return lrm_Agent.lambda_range;
        }

        private static void sendToLRM(int lambda_amt, List<string> nodes, string[] lambdaRange)
        {
            string request = prepareLCRequest(lambda_amt, nodes, lambdaRange);
            LRM_Agent.sendMessage(request);
        }

        private static string prepareLCRequest(int lambda_amt, List<string> nodes, string[] lambdaRange)
        {
            StringBuilder sb = new StringBuilder("LINK_CONNECTION_REQUEST-");
            sb.Append(lambda_amt);
            sb.Append("-");
            foreach (var node in nodes)
            {
                sb.Append(node);
                sb.Append("&");
            }
            sb.Remove(sb.Length - 1, 1);
            if (lambdaRange != null)
            {
                sb.Append("-");
                sb.Append(lambdaRange[0]);
                sb.Append("-");
                sb.Append(lambdaRange[1]);
            }
            return sb.ToString();
        }

        private string prepareReleaseRequest(RouteTableQuery routeTableQuery)
        {
            if (routeTableQuery.RouteTableRows.Count > 0)
            {
                string[] lambdas = routeTableQuery.RouteTableRows[0].Lambda_Range;
                StringBuilder sb = new StringBuilder("RELEASE-");
                sb.Append(lambdas[0]);
                sb.Append("&");
                sb.Append(lambdas[1]);
                sb.Append("-");
                foreach (var row in routeTableQuery.RouteTableRows)
                {
                    sb.Append(row.NodeName);
                    sb.Append("&");
                }
                sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
            return null;
        }

        private string prepareNCCRequest(bool isStoredRoute)
        {
            StringBuilder sb = new StringBuilder("Connection request_rsp@");
            if (isStoredRoute)
            {
                foreach (MessagePackage m in storedRoutes.Keys)
                {
                    if (messagePackage.SourceIP.Equals(m.SourceIP) && messagePackage.DestinationIP.Equals(m.DestinationIP))
                    {
                        RouteTableQuery routeTableQuery = null;
                        storedRoutes.TryGetValue(m, out routeTableQuery);
                        sb.Append(routeTableQuery.RouteTableRows[0].Lambda_Range[0]);
                        sb.Append("-");
                        sb.Append(routeTableQuery.RouteTableRows[0].Lambda_Range[1]);
                    }
                }
            }
            else
            {
                sb.Append(lambda_range[0]);
                sb.Append("-");
                sb.Append(lambda_range[1]);
            }
            return sb.ToString();
        }

        private static void Send(Socket handler, StateObject st, RouteTableRow data, bool release)
        {
            try
            {
                if (release)
                {
                    AddLog($"Sending Connection request_req(release) to node: {data.NodeName}", LogType.Action);
                }
                else
                {
                    AddLog($"Sending Connection request_req to node: {data.NodeName}", LogType.Action);
                }
                byte[] byteData = data.ToBytes();
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            } catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace,LogType.Information);
            }
            
        }

        public static void SendCallback(IAsyncResult ar)
        {
            Socket handler = null;
            try
            {
                handler = (Socket)ar.AsyncState;
                int bytesSend = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace, LogType.Error);
            }
            finally
            {
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
        }

        private bool checkIfStoredPackage(MessagePackage messagePackage)
        {
            if (storedRoutes.Count > 0)
            {
                foreach (MessagePackage m in storedRoutes.Keys)
                {
                    if (messagePackage.SourceIP.Equals(m.SourceIP) && messagePackage.DestinationIP.Equals(m.DestinationIP) && messagePackage.desired_bandwidth.Equals(m.desired_bandwidth))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public void SaveSocket(Socket socket, string address)
        {
            clientSockets.TryAdd(address, socket);
        }

        private void ConnectToCC(Config config, Socket s)
        {
            AddLog($"Connecting to CC at {config.CCConnectionData.ipAddress}:{config.CCConnectionData.port}", LogType.Information);

            try
            {
                //Socket socket = new Socket(config.IP.AddressFamily, SocketType.Stream,
                //ProtocolType.Tcp);

                s.Connect(new IPEndPoint(config.CCConnectionData.ipAddress, config.CCConnectionData.port));

                s.Send(Encoding.ASCII.GetBytes($"HELLO-{Config.Name}"));
                AddLog("Estabilished connection with CC", LogType.Information);

                SaveSocket(s, "CC");
            }
            catch (Exception e)
            {
                AddLog($"Failed to connect to CC {e.Message + e.StackTrace}", LogType.Error);
            }
        }

        private void sendMessageToCC(RouteTableQuery routeTableQuery)
        {
            var networkObject = clientSockets.First(x => x.Key == "CC");
            Socket CCSocket = networkObject.Value;
            string request = prepareCCRequest(routeTableQuery);
            if (CCSocket == null || !CCSocket.Connected)
            {
                AddLog("Connection to CC not established", LogType.Error);
                return;
            }
            try
            {
                CCSocket.Send(Encoding.ASCII.GetBytes(request));
            }
            catch (Exception e)
            {
                AddLog($"Unable to send request: {e.Message + e.StackTrace}", LogType.Error);
            }
        }

        private string prepareCCRequest(RouteTableQuery routeTableQuery)
        {
            if (routeTableQuery != null)
            {
                //usuwanie
                StringBuilder sb = new StringBuilder("OK_DEL-");
                sb.Append(Config.Name).Append("-");
                foreach (var node in routeTableQuery.RouteTableRows)
                {
                    sb.Append(node.NodeName).Append("&");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("-");
                sb.Append("Connection request_rsp(release)");
                return sb.ToString();

            }
            else
            {
                //dodawanie
                StringBuilder sb = new StringBuilder("OK-");
                sb.Append(Config.Name).Append("-");
                foreach (var node in RC_Agent.ROUTE_TABLE_QUERY.RouteTableRows)
                {
                    sb.Append(node.NodeName).Append("&");
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append("-");
                sb.Append("Connection request_rsp");
                return sb.ToString();
            }
        }

        private void sendToSubCC()
        {
            foreach (var subCC in subNetworks)
            {
                mre.Reset();
                try
                {
                    var networkObject = clientSockets.First(x => x.Key == subCC.NodeName);
                    Socket subCCSocket = networkObject.Value;
                    MessagePackage request = messagePackage;
                    request.Payload = prepareSubCCRequest(subCC);
                    subCCSocket.Send(request.ToBytes());
                    AddLog($"Sending Connection request_req to sub CC {subCC.NodeName}", LogType.Information);
                    waitForSubCC(subCCSocket);
                    Thread.Sleep(1000);
                    mre.WaitOne();
                }
                catch (Exception e)
                {
                    AddLog(e.Message + e.StackTrace, LogType.Information);
                }

            }
            Thread.Sleep(5000);
        }

        public void waitForSubCC(Socket socket)
        {
            try
            {
                StateObject state = new StateObject();
                state.tempSocket = socket;
                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackCC), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void deleteRouteFromsubCC(MessagePackage package)
        {
            foreach (var subCC in subNetworks)
            {
                mre1.Reset();
                try
                {
                    var networkObject = clientSockets.First(x => x.Key == subCC.NodeName);
                    Socket subCCSocket = networkObject.Value;
                    MessagePackage request = package;
                    request.Payload = "Connection request_req(release)";
                    subCCSocket.Send(request.ToBytes());
                    AddLog($"Sending Connection request_req(release) to sub CC: {subCC.NodeName}", LogType.Information);
                    waitForSubCC(subCCSocket);
                    Thread.Sleep(1000);
                    mre1.WaitOne();      
                }
                catch (Exception e)
                {
                    AddLog(e.Message + e.StackTrace, LogType.Information);
                }
                Thread.Sleep(5000);

            }
        }

        public void ReceiveCallbackCC(IAsyncResult ar)
        {
            try
            {
                StateObject state = ar.AsyncState as StateObject;
                Socket client = state.tempSocket;
                int amount = client.EndReceive(ar);
                byte[] array = new byte[amount];
                Array.Copy(state.buffer, array, amount);
                string response = Encoding.ASCII.GetString(array.ToArray());
                handlesubCCResponse(response);
                AddLog($"Received {response.Split("-")[3]} from subnetwork's CC", LogType.Information);
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackCC), state);
                mre.Set();
                mre1.Set();
            }
            catch (Exception e)
            {
                AddLog(e.Message + e.StackTrace, LogType.Information);
            }
        }

        private void handlesubCCResponse(string response)
        {
            string subCC = response.Split("-")[1];
            string[] nodes = response.Split("-")[2].Split("&");
            foreach (var node in nodes)
            {
                node.Replace('R', 'r');
            }
            subCCRoutes.Add(subCC, nodes);
        }

        private string prepareSubCCRequest(RouteTableRow routeTableRow)
        {
            StringBuilder sb = new StringBuilder("Connection request-");
            sb.Append(lambda_range[0]);
            sb.Append("@");
            sb.Append(lambda_range[1]);
            sb.Append("-");
            sb.Append(routeTableRow.PortIn);
            sb.Append("@");
            sb.Append(routeTableRow.PortOut);
            sb.Append("-");
            sb.Append(lambda_amt);
            return sb.ToString();
        }

        private string handlePayload(string payload)
        {
            if (payload == "Connection request_req(release)")
            {
                return payload;
            }
            string[] splitted = payload.Split("-");
            subLambdaRange = splitted[1].Split("@");
            subPortIn = ushort.Parse(splitted[2].Split("@")[0]);
            subPortOut = ushort.Parse(splitted[2].Split("@")[1]);
            subPortNodes.TryGetValue(subPortIn, out subSource);
            subPortNodes.TryGetValue(subPortOut, out subDestination);
            subLambdaAmt = int.Parse(splitted[3]);
            return splitted[0];
        }

        static async Task getResponseFromAPI(string sitehttp)
        {
            HttpClient client = new HttpClient();
            try
            {
                string responseBody = await client.GetStringAsync(sitehttp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }

        private string prepareAPIRequest(string action, RouteTableQuery routeTableQuery)
        {
            RouteTableQuery route = null;
            if (routeTableQuery != null)
            {
                route = routeTableQuery;

            }
            else
            {
                route = RC_Agent.ROUTE_TABLE_QUERY;
            }
            List<RouteTableRow> subNodes = new List<RouteTableRow>();
            foreach (var node in route.RouteTableRows)
            {
                if (node.NodeName.Substring(0, 2) == "CC")
                {
                    subNodes.Add(node);
                }
            }
            while (subNodes.Count() != subCCRoutes.Count())
            {
            }
            StringBuilder sb = new StringBuilder("http://127.0.0.1:8050/api/");
            sb.Append(action).Append("/");
            sb.Append(route.SourceIP).Append("$");
            foreach (var node in route.RouteTableRows)
            {
                if (node.NodeName.Substring(0, 2) == "CC")
                {
                    string[] subRoute = null;
                    subCCRoutes.TryGetValue(node.NodeName, out subRoute);
                    foreach (var subNode in subRoute)
                    {
                        sb.Append(subNode.Replace('R', 'r')).Append("$");
                    }
                }
                else
                {
                    string n = node.NodeName.Replace('R', 'r');
                    sb.Append(n).Append("$");
                }
            }
            sb.Append(route.DestinationIP);
            return sb.ToString();
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

