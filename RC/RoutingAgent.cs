using System;
using System.Collections.Generic;
using System.Text;

namespace Dijkstra
{
    class RoutingAgent
    {
        public RoutingAgent()
        {
            //pass
        }

        public int CalculateSlots(double bandwidth, string src, string dst)
        {
            bandwidth /= 1000000000;
            bandwidth *= 2;
            int modulatinIndex = PickModulation(src, dst);
            bandwidth /= Math.Log2(modulatinIndex);
            bandwidth += 10;
            Listener.AddLog($"Calculated slots' number: {(int)Math.Ceiling(bandwidth / 12.5)}", ConsoleColor.Gray);
            return (int)Math.Ceiling(bandwidth / 12.5);
        }

        private int PickModulation(string src, string dst)
        {
            if ((src == "H2" && dst == "H3") || (src == "H3" && dst == "H2"))
            {
                Listener.AddLog("Picked modulation: 64QAM", ConsoleColor.White);
                return 64;
            }
            else
            {
                Listener.AddLog("Picked modulation: 16QAM", ConsoleColor.White);
                return 16;
            }
        }
    }
}
