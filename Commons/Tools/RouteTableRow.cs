using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Commons.Tools
{
    public class RouteTableRow
    {
        public string NodeName { get; set; }
        public string[] Lambda_Range { get; set; }
        public ushort PortIn { get; set; }
        public ushort PortOut { get; set; }
        public string action { get; set; }
        private int size { get; set; }

        public RouteTableRow()
        {
            Lambda_Range = null;
            size = 0;
            action = "ADD";
        }

        public byte[] ToBytes()
        {
            List<byte> list_of_bytes = new List<byte>();
            if (Lambda_Range != null)
            {
                size = action.Length + NodeName.Length + 2 + 2 + 4 + Lambda_Range.Length;
            } else
            {
                size = action.Length + NodeName.Length + 2 + 2 + 4;
            }
                
            list_of_bytes.AddRange(BitConverter.GetBytes(PortIn));
            list_of_bytes.AddRange(BitConverter.GetBytes(PortOut));
            list_of_bytes.AddRange(BitConverter.GetBytes(size));

            if (Lambda_Range != null)
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(Lambda_Range.Length));
                for (int i = 0; i < Lambda_Range.Length; i++)
                {
                    list_of_bytes.AddRange(BitConverter.GetBytes(Lambda_Range[i].Length));
                    list_of_bytes.AddRange(Encoding.ASCII.GetBytes(Lambda_Range[i]));
                }
            } else
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(0));
            }

            list_of_bytes.AddRange(Encoding.ASCII.GetBytes(action));
            list_of_bytes.AddRange(Encoding.ASCII.GetBytes(NodeName));

            return list_of_bytes.ToArray();
        }

        public static RouteTableRow FromBytes(byte[] bytes)
        {

            RouteTableRow routeTableRow = new RouteTableRow();

            routeTableRow.PortIn = BitConverter.ToUInt16(bytes);
            routeTableRow.PortOut = BitConverter.ToUInt16(bytes, 2);
            routeTableRow.size = BitConverter.ToInt32(bytes, 4);

            int tempeleCount = BitConverter.ToInt32(bytes, 8);
            int startingIndex = 12;
            if (tempeleCount != 0)
            {
                routeTableRow.Lambda_Range = new string[2];
                for (int i = 0; i < tempeleCount; i++)
                {
                    List<byte> received = new List<byte>();
                    int number_of_chars = BitConverter.ToInt32(bytes, startingIndex);
                    startingIndex += 4;
                    received.AddRange(bytes.ToList().GetRange(startingIndex, number_of_chars));
                    routeTableRow.Lambda_Range[i] = Encoding.ASCII.GetString(received.ToArray());
                    startingIndex += number_of_chars;
                }
            }
            
            List<byte> action_payload = new List<byte>();
            action_payload.AddRange(bytes.ToList().GetRange(startingIndex, 3));
            routeTableRow.action = Encoding.ASCII.GetString(action_payload.ToArray());
            startingIndex += 3;

            List<byte> receive_payload = new List<byte>();
            int end_of_payload = 0;
            if (tempeleCount != 0)
            {
                end_of_payload = routeTableRow.size - 2 - 2 - 4 - routeTableRow.Lambda_Range.Length - 3;
            } else
            {
                end_of_payload = routeTableRow.size - 2 - 2 - 4 - 3;
            }
            receive_payload.AddRange(bytes.ToList().GetRange(startingIndex, end_of_payload));

            routeTableRow.NodeName = Encoding.ASCII.GetString(receive_payload.ToArray());

            return routeTableRow;
        }

        public int getBytes()
        {
            return ToBytes().Length;
        }

        public bool Equals(RouteTableRow tmp)
        {
            if(this.NodeName == tmp.NodeName && this.Lambda_Range.SequenceEqual(tmp.Lambda_Range) && this.PortIn == tmp.PortIn && this.PortOut == tmp.PortOut)
            {
                return true;
            }
            return false;
        }

        public string TableRow_Information()
        {
            return $"Node: {NodeName} PortIn:{PortIn} PortOut:{PortOut} Frequency range: {String.Join(" - ", Lambda_Range).Replace(",", ".")} [THz]";
        }
    }
}
