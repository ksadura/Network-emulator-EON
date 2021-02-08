using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
namespace Commons.Tools
{
    public class MessagePackage
    {
        public int package_size { get; set; }

        public double desired_bandwidth { get; set; }

        public IPAddress SourceIP { get; set; }

        public IPAddress DestinationIP { get; set; }

        public string SourceName { get; set; }

        public string DestinationName { get; set; }

        public string Payload { get; set; }

        public float power_level { get; set; }

        public int call_ID { get; set; }


        public MessagePackage()
        {
            package_size = 0;
            desired_bandwidth = 0;
            call_ID = 0;
            SourceIP = null;
            DestinationIP = null;
        }

        public byte[] ToBytes()
        {
            List<byte> list_of_bytes = new List<byte>();
            if (Payload == null)
            {
                package_size = 4 + 4 + 4 + 8;//ilość bajtów pakietu=wielkość pakietu+adres źródłowy+adres docelowy
            }
            else
                package_size = Payload.Length + 4 + 4 + 4 + 8;//ilość bajtów pakietu=wiadomości(1 bajt na znak)+wielkość pakietu+adres źródłowy+adres docelowy

            list_of_bytes.AddRange(BitConverter.GetBytes(desired_bandwidth));
            list_of_bytes.AddRange(BitConverter.GetBytes(package_size));
            if (SourceIP != null)
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(true));
                list_of_bytes.AddRange(SourceIP.GetAddressBytes());
            }
            else
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(false));
                list_of_bytes.AddRange(BitConverter.GetBytes(SourceName.Length));
                list_of_bytes.AddRange(Encoding.ASCII.GetBytes(SourceName));
            }
            if (DestinationIP != null)
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(true));
                list_of_bytes.AddRange(DestinationIP.GetAddressBytes());
            }
            else
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(false));
                list_of_bytes.AddRange(BitConverter.GetBytes(DestinationName.Length));
                list_of_bytes.AddRange(Encoding.ASCII.GetBytes(DestinationName));
            }
            list_of_bytes.AddRange(BitConverter.GetBytes(power_level));
            list_of_bytes.AddRange(Encoding.ASCII.GetBytes(Payload ?? ""));//wysyła pustego stringa "" jeżeli Payload=null

            list_of_bytes.AddRange(BitConverter.GetBytes(call_ID));

            return list_of_bytes.ToArray();
        }

        public static MessagePackage FromBytes(byte[] bytes)
        {

            MessagePackage package = new MessagePackage();

            package.desired_bandwidth = BitConverter.ToDouble(bytes);
            package.package_size = BitConverter.ToInt32(bytes,8);
            bool checker = BitConverter.ToBoolean(bytes, 12);
            int saved_index = 13;
            if (checker)
            {
                package.SourceIP = new IPAddress(new byte[] { bytes[13], bytes[14], bytes[15], bytes[16] });
                saved_index = 17;
            }
            else
            {
                package.SourceIP = null;
                int len = BitConverter.ToInt32(bytes, saved_index);
                List<byte> received = new List<byte>();
                received.AddRange(bytes.ToList().GetRange(saved_index+4, len));
                saved_index = saved_index + 4 + len;
                package.SourceName = Encoding.ASCII.GetString(received.ToArray());
            }
            bool checkerDest = BitConverter.ToBoolean(bytes, saved_index);
            saved_index += 1;
            if (checkerDest)
            {
                package.DestinationIP = new IPAddress(new byte[] { bytes[saved_index], bytes[saved_index+1], bytes[saved_index+2], bytes[saved_index+3] });
                saved_index = saved_index+4;
            }
            else
            {
                package.DestinationIP = null;
                int len = BitConverter.ToInt32(bytes, saved_index);
                List<byte> received = new List<byte>();
                received.AddRange(bytes.ToList().GetRange(saved_index + 4, len));
                saved_index = saved_index + 4 + len;
                package.DestinationName = Encoding.ASCII.GetString(received.ToArray());
            }
            package.power_level = BitConverter.ToSingle(bytes, saved_index);
            saved_index += 4;

            List<byte> receive_payload = new List<byte>();
            int end_of_payload = package.package_size - 4 - 4 - 4 - 8;
            receive_payload.AddRange(bytes.ToList().GetRange(saved_index, end_of_payload));

            package.Payload = Encoding.ASCII.GetString(receive_payload.ToArray());

            package.call_ID = BitConverter.ToInt32(bytes, saved_index + end_of_payload);

            return package;

        }
        public string Packet_Information()
        {
            string package;
            if (desired_bandwidth == 0 && Payload != null && Payload != "" && power_level != 0)
                package = $"ID połączenia: {call_ID} Adres źródłowy:{SourceIP}=>Adres docelowy:{DestinationIP} Wiadomość:{Payload} Moc:{power_level} [dBW]";
            else if (desired_bandwidth == 0 && Payload != null && Payload != "" && power_level == 0 && DestinationIP!=null && SourceIP!=null)
                package = $"ID połączenia: {call_ID} Adres źródłowy:{SourceIP}=>Adres docelowy:{DestinationIP} Wiadomość:{Payload}";
            else if (Payload == null || Payload == "")
                package = $"ID połączenia: {call_ID} Adres źródłowy:{SourceIP}=>Adres docelowy:{DestinationIP}";
            else if ((SourceIP == null || DestinationIP == null) && Payload != null && Payload != "" && desired_bandwidth == 0)
                package = $"ID połączenia: {call_ID} Nazwa hosta źródłowego:{SourceName}=>Nazwa hosta docelowego:{DestinationName} Wiadomość: {Payload}";
            else if ((SourceIP == null || DestinationIP == null) && Payload != null && Payload != "" && desired_bandwidth!=0)
                package = $"ID połączenia: {call_ID} Nazwa hosta źródłowego:{SourceName}=>Nazwa hosta docelowego:{DestinationName} Wiadomość: {Payload} Przepustowość: {desired_bandwidth} [b]";
            else
                package = $"ID połączenia: {call_ID} Przepustowość={desired_bandwidth} [b] Adres źródłowy:{SourceIP}=>Adres docelowy:{DestinationIP} Wiadomość:{Payload} Moc:{power_level} [dBW]";
            return package;
        }
    }
}
