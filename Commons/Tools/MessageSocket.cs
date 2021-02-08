using System;
using System.Net.Sockets;
using System.Text;
namespace Commons.Tools
{
    public class MessageSocket : Socket
    {
        public MessageSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) :
           base(addressFamily, socketType, protocolType)
        {
            ReceiveTimeout = 5000000;
        }
        public MessagePackage Receive()
        {
            var buffer = new byte[1024];
            int bytes = Receive(buffer);
            byte[] receivedBytes = new byte[bytes];
            Array.Copy(buffer, receivedBytes, bytes);
            if (Encoding.ASCII.GetString(buffer, 0, bytes).Substring(0, 9).Equals("KEEPALIVE"))
            {
                return null;
            }
            return MessagePackage.FromBytes(receivedBytes);
        }
    }
}
