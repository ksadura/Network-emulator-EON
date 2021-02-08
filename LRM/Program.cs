using System;
using System.Threading.Tasks;

namespace LRM
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Config.ReadConfig(args[0]);
                var lrm = Task.Run(() => new Listener().Start());
                var terminal = Task.Run(() => new Terminal().Start());

                lrm.Wait();
                terminal.Wait();
                //new Thread(new FiberCloud().StartCloud).Start();
            }
            else
            {
                //ConfigCloud.ReadConfig(@"C:\Users\kenic\Desktop\ASON_backup\tsst\CableCloud\Config.xml");
                //LinkResourceManager lrm = new LinkResourceManager();
                //(double f1, double f2, int i, int j) = lrm.ChooseLinks(4, "CC_S1&R8&CC_S2");
                //Console.WriteLine($"f1: {f1} f2: {f2}");
                //(double f11, double f22, int i1, int j1) = lrm.ChooseLinks(11, "CC_S2&R8&CC_S1");
                //Console.WriteLine($"f1: {f11} f2: {f22}");
                //lrm.ReleaseResources(f1.ToString(), f2.ToString(), "CC_S1", "R8");
                //lrm.ReleaseResources(f1.ToString(), f2.ToString(), "R8", "CC_S2");
                //(double f111, double f222, int i111, int j111) = lrm.ChooseLinks(7, "CC_S1&R8&CC_S2");
                //Console.WriteLine($"f1: {f111} f2: {f222}");

            }

        }
    }
}
