using System;
using System.Threading.Tasks;

namespace NCC
{
    public class Run
    {
        static void Main(string[] args)
        {
            Console.Title = "NCC";
            NCC nCC = new NCC();
            Task task;
            if (args.Length != 0) { task = Task.Run(() => nCC.StartNCC(new NCCConfig(args[1]))); }
            else { Console.WriteLine("Błędna śćieżka do pliku konfiguracyjnego");task = Task.Run(() => nCC.StartNCC(new NCCConfig("./NCCConfigExample.xml"))); }
            task.Wait();

        }
    }
}
