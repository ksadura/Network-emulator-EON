using System;
using System.Net;
namespace Host
{
    public class OtherHostsInfo
    {
        public IPAddress IP { get; set; }
        public string other_host_name { get; set; }
        public string[] lambdas { get; set; }
        public int call_ID { get; set; }
        public double bandwidth { get; set; }
        public OtherHostsInfo()
        {
        }

        public OtherHostsInfo(IPAddress IP, string other_host_name,string[] lambdas,int call_ID,double bandwidth)
        {
            this.IP = IP;
            this.other_host_name = other_host_name;
            this.lambdas = lambdas;
            this.call_ID = call_ID;
            this.bandwidth = bandwidth;
        }
    }
}
