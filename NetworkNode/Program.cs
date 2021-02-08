using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkNodes
{
    class Program
    {
        public static void Main(string[] args)
        {
            if(args.Length >0)
                new NetworkNode().Start(args[0]);
            Console.ReadKey();
        }
    }
}
