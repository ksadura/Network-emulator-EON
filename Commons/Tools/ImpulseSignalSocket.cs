using System;
using System.Net.Sockets;
using System.Text;

namespace Commons.Tools
{
    public class ImpulseSignalSocket: Socket
    {
        public ImpulseSignalSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) :
           base(addressFamily, socketType, protocolType)
        {
            ReceiveTimeout = 5000000;
        }
        public ImpulseSignalPackage Receive()
        {
            var buffer = new byte[1024];
            int bytes = Receive(buffer);
            byte[] receivedBytes = new byte[bytes];
            Array.Copy(buffer, receivedBytes, bytes);
            if (Encoding.ASCII.GetString(receivedBytes, 0, bytes).Substring(0, 9).Equals("KEEPALIVE"))
            {
                return null;
            }

            return ImpulseSignalPackage.FromBytes(receivedBytes);
        }

        public int Send(ImpulseSignalPackage package)
        {
            return Send(package.ToBytes());
        }
    }
}
