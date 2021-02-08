using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Commons.Tools
{
    public class ImpulseSignalPackage
    {
        public int ID { get; set; }

        public int package_size { get; set; }

        public ushort Port { get; set; }

        public string Payload { get; set; }

        public string[] Lambda_Range { get; set; }
        public IPAddress PrevIP { get; set; }

        public ImpulseSignalPackage()
        {
            Lambda_Range = new string[2];
            package_size = 0;
        }

        public byte[] ToBytes()
        {
            List<byte> list_of_bytes = new List<byte>();
            package_size = Payload.Length + 4 + 4 + 2 + 4 + Lambda_Range.Length;//ilość bajtów pakietu=wiadomości(1 bajt na znak)+ID+wielkość pakietu+adres poprzedniego+adres źródłowy+adres docelowy+port

            list_of_bytes.AddRange(BitConverter.GetBytes(ID));
            list_of_bytes.AddRange(BitConverter.GetBytes(package_size));
            list_of_bytes.AddRange(BitConverter.GetBytes(Port));
            list_of_bytes.AddRange(PrevIP.GetAddressBytes());

            list_of_bytes.AddRange(BitConverter.GetBytes(Lambda_Range.Length));
            for(int i=0; i<Lambda_Range.Length; i++)
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(Lambda_Range[i].Length));
                list_of_bytes.AddRange(Encoding.ASCII.GetBytes(Lambda_Range[i]));
            }
            list_of_bytes.AddRange(Encoding.ASCII.GetBytes(Payload ?? ""));//wysyła pustego stringa "" jeżeli Payload=null

            return list_of_bytes.ToArray();
        }

        public static ImpulseSignalPackage FromBytes(byte[] bytes)
        {

            ImpulseSignalPackage package = new ImpulseSignalPackage();

            package.ID = BitConverter.ToInt32(bytes);
            package.package_size = BitConverter.ToInt32(bytes, 4);
            package.Port = BitConverter.ToUInt16(bytes, 8);
            package.PrevIP = new IPAddress(new byte[] { bytes[10], bytes[11], bytes[12], bytes[13] });

            int size = BitConverter.ToInt32(bytes, 14);
            int startingIndex = 18;
            for (int i=0; i < size; i++)
            {
                List<byte> received = new List<byte>();
                int number_of_chars = BitConverter.ToInt32(bytes, startingIndex);
                startingIndex += 4;
                received.AddRange(bytes.ToList().GetRange(startingIndex, number_of_chars));
                package.Lambda_Range[i] = Encoding.ASCII.GetString(received.ToArray());
                startingIndex += number_of_chars;
            }
            List<byte> receive_payload = new List<byte>();
            int end_of_payload = package.package_size - 4 - 4 - 2 - 4 - package.Lambda_Range.Length;
            receive_payload.AddRange(bytes.ToList().GetRange(startingIndex, end_of_payload));

            package.Payload = Encoding.ASCII.GetString(receive_payload.ToArray());

            return package;

        }
        public string Packet_Information()
        {
            return $"ID sygnału:{ID} Wiadomość:{Payload} Zakres szczelin: {String.Join(" - ",Lambda_Range).Replace(",", ".")} [THz]";
        }
        
    }
}
