using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Host.MPLSRelated
{
    public class MPLSPackage
    {
        public int ID { get; set; }

        public int package_size { get; set; }

        //public ushort TTL { get; set; }

        public IPAddress PrevIP { get; set; }

        public IPAddress SourceIP { get; set; }

        public IPAddress DestinationIP { get; set; }

        public ushort Port { get; set; }

        public string Payload { get; set; }

        public LabelStack label_stack { get; set; }

        //private const int Starting_TTL = 255;

        public MPLSPackage()
        {
            //TTL = Starting_TTL;
            package_size = 0;
            label_stack = new LabelStack();
        }

        public byte[] ToBytes()
        {
            List<byte> list_of_bytes = new List<byte>();
            package_size = label_stack.GetBytes_Length()+Payload.Length+4+4+4+4+4+2;//ilość bajtów pakietu=stosy etykiet+wiadomości(1 bajt na znak)+ID+wielkość pakietu+adres poprzedniego+adres źródłowy+adres docelowy+port

            list_of_bytes.AddRange(label_stack.ToBytes());
            list_of_bytes.AddRange(BitConverter.GetBytes(ID));
            list_of_bytes.AddRange(BitConverter.GetBytes(package_size));
            list_of_bytes.AddRange(PrevIP.GetAddressBytes()); //to zostalo dodane zamiast TTL
            //list_of_bytes.AddRange(BitConverter.GetBytes(TTL));
            list_of_bytes.AddRange(BitConverter.GetBytes(Port));
            list_of_bytes.AddRange(SourceIP.GetAddressBytes());
            list_of_bytes.AddRange(DestinationIP.GetAddressBytes());
            list_of_bytes.AddRange(Encoding.ASCII.GetBytes(Payload ?? ""));//wysyła pustego stringa "" jeżeli Payload=null

            return list_of_bytes.ToArray();
        }

        public static MPLSPackage FromBytes(byte[] bytes)
        {

            MPLSPackage package = new MPLSPackage();

            package.label_stack = LabelStack.FromBytes(bytes);
            int stack_bytes_count = package.label_stack.GetBytes_Length();

            package.ID = BitConverter.ToInt32(bytes,stack_bytes_count);
            package.package_size = BitConverter.ToInt32(bytes, stack_bytes_count+4);
            package.PrevIP = new IPAddress(new byte[] { bytes[stack_bytes_count + 8], bytes[stack_bytes_count + 9], bytes[stack_bytes_count + 10], bytes[stack_bytes_count + 11] }); // to zostalo dodane zamiast TTL
            //package.TTL = BitConverter.ToUInt16(bytes, stack_bytes_count + 8);
            package.Port = BitConverter.ToUInt16(bytes, stack_bytes_count + 12);

            package.SourceIP = new IPAddress(new byte[]{bytes[stack_bytes_count+14], bytes[stack_bytes_count + 15], bytes[stack_bytes_count + 16], bytes[stack_bytes_count + 17]});
            package.DestinationIP = new IPAddress(new byte[] {bytes[stack_bytes_count + 18], bytes[stack_bytes_count + 19], bytes[stack_bytes_count + 20], bytes[stack_bytes_count + 21]});


            List<byte> receive_payload = new List<byte>();
            int end_of_payload = package.package_size - stack_bytes_count - 4 - 4 - 4 - 4 - 4 - 2;
            receive_payload.AddRange(bytes.ToList().GetRange(stack_bytes_count+22, end_of_payload));
                
            package.Payload = Encoding.ASCII.GetString(receive_payload.ToArray());

            return package;

        }
        public string Packet_Information()
        {
            string package;
            if (package_size==0)
                package = $"Rozmiar pakietu=NA ID pakietu:{ID} Adres źródłowy:{SourceIP}=>Adres docelowy:{DestinationIP} Wiadomość:{Payload}";
            else
                package = $"Rozmiar pakietu={package_size} [B] ID pakietu:{ID} Adres źródłowy:{SourceIP}=>Adres docelowy:{DestinationIP} Wiadomość:{Payload}";
            return package;
        }
        public Label checkLabel()
        {
            if (!label_stack.IsEmpty())
            {
                return label_stack.stack_of_labels.Peek();
            }
            else
                return null;
        }
        //podmienia label na szczycie!!!UWAGA!!!tracisz element z góry
        public void swapLabel(Label label)
        {

            if (!label_stack.IsEmpty())
            {
                label_stack.stack_of_labels.Pop();
                label_stack.stack_of_labels.Push(label);
            }
        }
        public void pushLabel(Label label)
        {
            label_stack.stack_of_labels.Push(label);
        }
        public Label popLabel()
        {
            if (!label_stack.IsEmpty())
            {
                return label_stack.stack_of_labels.Pop();
            }
            else
                return null;

        }
    }
}
