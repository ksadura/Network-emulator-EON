using System;
using static NetworkNodes.NetworkNode;
using Commons.Tools;
using System.Linq;

namespace NetworkNodes
{
    public class NetworkNodeRoutingTables
    {
        public RouteTable routeTable { get; set; }

        public NetworkNodeRoutingTables()
        {
            routeTable = new RouteTable();
        }

        public string AddTableEntry(RouteTableRow package)
        {
            AddLog($"Received Connection request_req from CC: {package.TableRow_Information()}", LogType.Information);
            bool modified = false;
            foreach (var row in routeTable.Rows)
            {
                if (package.Lambda_Range.SequenceEqual(row.Lambda_Range))
                {
                    AddLog($"Update configuration {row.TableRow_Information()} to {package.TableRow_Information()}", LogType.Update);
                    row.PortIn = package.PortIn;
                    row.PortOut = package.PortOut;
                    modified = true;
                    break;
                }
            }
            if (!modified)
            {
                AddLog($"Add new configuration {package.TableRow_Information()}", LogType.Add);
                routeTable.Rows.Add(package);
            }
            return "OK";
        }

        public string DeleteTableEntry(RouteTableRow package)
        {
            AddLog($"Received Connection request_req(release) from CC: {package.TableRow_Information()}", LogType.Information);
            foreach (RouteTableRow row in routeTable.Rows)
            {
                if (row.Equals(package))
                {
                    AddLog("Configuration deleted", LogType.Remove);
                    routeTable.Rows.Remove(package);
                    return "OK_DELETED";
                }
            }
            return null;
        }

        private void AddLog(string log, LogType logType)
        {
            switch (logType)
            {
                case LogType.Update:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case LogType.Add:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;
                case LogType.Remove:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
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
