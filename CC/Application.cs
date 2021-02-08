using System;
using System.Threading.Tasks;

namespace CC
{
    class Application
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Config config = Config.ParseConfig(args[0]);
                CC cc = new CC();
                Task task;
                task = Task.Run(() => cc.StartCC(config));
                task.Wait();
            }
        }
    }
}
