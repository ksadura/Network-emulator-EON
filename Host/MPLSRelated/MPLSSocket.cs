using System;
using System.Net.Sockets;
using System.Text;

namespace Host.MPLSRelated
{
    public class MPLSSocket: Socket
    {
        public MPLSSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) :
           base(addressFamily, socketType, protocolType)
        {
            ReceiveTimeout = 5000000;
        }
        public MPLSPackage Receive()
        {
            var buffer = new byte[256];
            int bytes = Receive(buffer);
            if (Encoding.ASCII.GetString(buffer, 0, bytes).Substring(0, 9).Equals("KEEPALIVE"))
            {
                return null;
            }

            return MPLSPackage.FromBytes(buffer);
        }
    }
}
