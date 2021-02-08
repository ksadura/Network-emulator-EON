using System;
using System.Threading;
using System.Linq;
using System.Net;

namespace Dijkstra
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Config.ReadConfig(args[0]);
                new Listener().StartRoutingController();
            }
        }
    }
}
