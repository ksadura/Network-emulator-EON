using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Commons.Tools;
using static NetworkNodes.NetworkNode;

namespace NetworkNodes
{
    public class PackageSwitch
    {
        private NetworkNodeRoutingTables RoutingTables { get; set; }

        public ImpulseSignalPackage RouteSignalPackage(ImpulseSignalPackage package, NetworkNodeRoutingTables networkNodeRoutingTables)
        {
            RoutingTables = networkNodeRoutingTables;

            ImpulseSignalPackage routedPackage = null;

            try
            {
                routedPackage = RoutePackage(package);
            }
            catch (Exception e)
            {
                AddLog($"Exception: {e.StackTrace}", LogType.Error);
                return null;
            }

            if (routedPackage == null)
            {
                AddLog($"I don't know how to send signal.", LogType.Error);
                return null;
            }
            return routedPackage;
        }

        private ImpulseSignalPackage RoutePackage(ImpulseSignalPackage package)
        {
            ImpulseSignalPackage routedPackage = package;
            RouteTableRow tmpRow = null;

            foreach (var row in RoutingTables.routeTable.Rows)
            {
                if (row.PortIn.Equals(package.Port) && row.Lambda_Range.SequenceEqual(package.Lambda_Range))
                {
                    tmpRow = row;
                }
            }
            AddLog($"Looking for matching configuration...", LogType.Information);
            if (tmpRow == null)
            {
                AddLog($"Couldn't find matching entry!", LogType.Error);
                foreach (var row in RoutingTables.routeTable.Rows)
                {
                    AddLog(row.TableRow_Information(), LogType.Error);
                }
                return null;
            }
            
            routedPackage.Port = (ushort)Convert.ToUInt32(tmpRow.PortOut);
            routedPackage.PrevIP = NetworkNodeConfig.NodeAddress;
            AddLog($"Impuls with ID={routedPackage.ID} routed to output port: {routedPackage.Port}", LogType.Route);
            return routedPackage;
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
                case LogType.Action:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogType.Route:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
            }
            log = $"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {log}";
            Console.WriteLine(log);
        }
    }
}
