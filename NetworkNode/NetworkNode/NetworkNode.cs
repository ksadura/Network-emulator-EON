using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Commons.Tools;

namespace NetworkNodes
{
    public partial class NetworkNode
    {
        public NetworkNodeConfig Config { get; set; }

        public ImpulseSignalSocket ConnectedSocket { get; set; }

        public PackageSwitch packageSwitch { get; set; }

        public ManagementAgent ManagementAgent { get; set; }


        public NetworkNode()
        {
            packageSwitch = new PackageSwitch();
        }

        public void Start(string configPath)
        {
            AddLog("Starting a node...", LogType.Information);

            try
            {
                Config = NetworkNodeConfig.ParseConfig(configPath);
                ManagementAgent = new ManagementAgent(Config);
            }
            catch (Exception e)
            {
                AddLog($"Exception: {e.Message}", LogType.Error);
                return;
            }
            

            ConnectToCloud();
            ManagementAgent.StartTask();

            while (true)
            {
                while (ConnectedSocket == null || !ConnectedSocket.Connected)
                {
                    AddLog("Retrying connection to cable cloud...", LogType.Information);
                    ConnectToCloud();
                }

                try
                {
                    var package = ConnectedSocket.Receive();

                    AddLog($"Received impulse: {package.ID} at port {package.Port}", LogType.Received);

                    Task.Run(() => HandlePackage(package));
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            AddLog("Connection to Cloud broken!", LogType.Error);
                            continue;
                        }

                        else
                        {
                            AddLog($"{e.Source}: {e.SocketErrorCode}", LogType.Error);
                        }
                    }
                }
            }
        }

        private void ConnectToCloud()
        {
            AddLog($"Connecting to cable cloud at {Config.CloudAddress}:{Config.CloudPort}", LogType.Information);

            try
            {
                ImpulseSignalSocket socket = new ImpulseSignalSocket(Config.CCAddress.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp);

                socket.Connect(new IPEndPoint(Config.CloudAddress, Config.CloudPort));

                socket.Send(Encoding.ASCII.GetBytes($"HELLO-{NetworkNodeConfig.NodeAddress}"));
                AddLog("Estabilished connection with cable cloud", LogType.Information);

                ConnectedSocket = socket;

                Task.Run(async () =>
                {
                    while (true)
                    {
                        var connectionGood = ConnectedSocket != null && ConnectedSocket.Connected;
                        if (connectionGood)
                        {
                        }
                        else
                        {
                            AddLog("Connection to cable cloud broken!", LogType.Error);
                            break;
                        }
                    }
                });
            }
            catch (Exception)
            {
                AddLog("Failed to connect to cable cloud", LogType.Error);
            }
        }

        private void HandlePackage(ImpulseSignalPackage package)
        {
            ImpulseSignalPackage routedPackage = null;
            routedPackage = packageSwitch.RouteSignalPackage(package, ManagementAgent.RoutingTable);
            if (routedPackage == null)
            {
                return;
            }
            try
            {
                ConnectedSocket.Send(routedPackage);
            }
            catch (Exception e)
            {
                AddLog($"Signal {routedPackage} not sent correctly: {e.Message}", LogType.Error);
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
                case LogType.Received:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
            }
            log = $"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {log}";
            Console.WriteLine(log);
        }
    }
}