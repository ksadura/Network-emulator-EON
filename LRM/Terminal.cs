using System;
using System.Threading;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace LRM
{
    class Terminal
    {
        private const string SPOIL = "spoil";
        private const string RESTORE = "restore";
        private string[] parameters;
        private string[] methods;
        private string[] routers = Config.Nodes.Split(" ");

        private static Socket socketToCC;
        private static Socket socketToRC;

        public Terminal() => methods = new string[] { SPOIL, RESTORE };

        public void Start()
        {
            while (true)
            {
                while (true)
                {
                    string s = Console.ReadLine();
                    parameters = s.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parameters.Length != 3 || !methods.Contains(parameters[0]))
                    {
                        Console.WriteLine("Bad syntax. Try again");
                    }
                    else if (!routers.Contains(parameters[1]) || !routers.Contains(parameters[2]))
                        Console.WriteLine("Unavailable routers");
                    else { break; }
                }
                switch (parameters[0].ToLower())
                {
                    case SPOIL:
                        Socket CC = Listener.ClientSockets[Config.CCName];
                        Socket RC = Listener.ClientSockets[Config.RCName];
                        string edge = parameters[1].ToLower() + "$" + parameters[2].ToLower();
                        Task.Run(() => getResponse("http://127.0.0.1:8050/api/breakdown/" + edge));
                        NotifyRC(in RC, parameters[1].ToUpper(), parameters[2].ToUpper());
                        Thread.Sleep(250);
                        SendError(in CC, parameters[1].ToUpper(), parameters[2].ToUpper());
                        break;
                    case RESTORE:
                        //implemented soon
                        break;
                    default:
                        Console.WriteLine("default");
                        break;
                }
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
                //pass
            }
        }
        public static void SendError(in Socket socket, string nodeOne, string nodeTwo)
        {
            socketToCC = socket;
            string info = $"ERROR-{nodeOne}&{nodeTwo}";
            byte[] data = Encoding.ASCII.GetBytes(info);

            socketToCC.BeginSend(data, 0, data.Length, 0, new AsyncCallback(Callback), socketToCC);
            AddLog("LRM> [Link Connection Request (response)]: Notifing CC about link removal", ConsoleColor.DarkYellow);
        }

        public static void NotifyRC(in Socket socket, string nodeOne, string nodeTwo)
        {
            socketToRC = socket;
            string info = $"ERROR {nodeOne}&{nodeTwo}";
            byte[] data = Encoding.ASCII.GetBytes(info);

            socketToRC.BeginSend(data, 0, data.Length, 0, new AsyncCallback(Callback), socketToRC);
            AddLog("LRM> [Local Topology]: Notifing RC about link removal", ConsoleColor.DarkYellow);
        }

        private static void Callback(IAsyncResult ar)
        {
            try
            {
                Socket handler = ar.AsyncState as Socket;
                int num = handler.EndSend(ar);
            }
            catch (Exception)
            {
                AddLog("Couldn't send error message!", ConsoleColor.Red);
            }

        }
        //Logging info
        private static void AddLog(object s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            if (s is string)
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]; {s}");
            Console.ResetColor();
        }

    }
}