using System;
using System.Collections.Generic;
using System.Net;

namespace Commons.Tools
{
    public class RouteTableQuery
    {
        public IPAddress SourceIP { get; set; }
        public IPAddress DestinationIP { get; set; }
        public int Lambda_Amount { get; set; }
        public List<RouteTableRow> RouteTableRows { get; set; }
        public ushort subPortIn { get; set; }
        public ushort subPortOut { get; set; }
        public double bandwith { get; set; }

    public RouteTableQuery()
        {
            RouteTableRows = new List<RouteTableRow>();
            subPortIn = 0;
            subPortOut = 0;
            bandwith = 0;
        }

        public byte[] ToBytes()
        {
            List<byte> list_of_bytes = new List<byte>();

            list_of_bytes.AddRange(SourceIP.GetAddressBytes());
            list_of_bytes.AddRange(DestinationIP.GetAddressBytes());
            list_of_bytes.AddRange(BitConverter.GetBytes(Lambda_Amount));
            list_of_bytes.AddRange(BitConverter.GetBytes(RouteTableRows.Count));
            list_of_bytes.AddRange(BitConverter.GetBytes(subPortIn));
            list_of_bytes.AddRange(BitConverter.GetBytes(subPortOut));
            list_of_bytes.AddRange(BitConverter.GetBytes(bandwith));
            foreach (RouteTableRow row in RouteTableRows)
            {
                list_of_bytes.AddRange(BitConverter.GetBytes(row.getBytes()));
                list_of_bytes.AddRange(row.ToBytes());
            }

            return list_of_bytes.ToArray();
        }

        public static RouteTableQuery FromBytes(byte[] bytes)
        {

            RouteTableQuery routeTableQuery = new RouteTableQuery();

            routeTableQuery.SourceIP = new IPAddress(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3] });
            routeTableQuery.DestinationIP = new IPAddress(new byte[] { bytes[4], bytes[5], bytes[6], bytes[7] });
            routeTableQuery.Lambda_Amount = BitConverter.ToInt32(bytes, 8);

            int tempeleCount = BitConverter.ToInt32(bytes, 12);
            routeTableQuery.subPortIn = BitConverter.ToUInt16(bytes, 16);
            routeTableQuery.subPortOut = BitConverter.ToUInt16(bytes, 18);
            routeTableQuery.bandwith = BitConverter.ToDouble(bytes, 20);
            int startingIndex = 28;
            for (int i = 0; i < tempeleCount; i++)
            {
                int rowSize = BitConverter.ToInt32(bytes, startingIndex);
                startingIndex += 4;
                byte[] rowBytes = new byte[rowSize];
                Buffer.BlockCopy(bytes, startingIndex, rowBytes, 0, rowSize);
                RouteTableRow tmp_row = new RouteTableRow();
                tmp_row = RouteTableRow.FromBytes(rowBytes);
                routeTableQuery.RouteTableRows.Add(tmp_row);
                startingIndex += rowSize;
            }

            return routeTableQuery;
        }

        public List<string> getRouteNodes()
        {
            List<string> routeNodes = new List<string>();
            foreach (var row in RouteTableRows)
            {
                routeNodes.Add(row.NodeName);
            }
            return routeNodes;
        }

        public string TableQuery_Information()
        {
            return $"SourceIP:{SourceIP} DestinationIP:{DestinationIP} Slots amount:{Lambda_Amount}";
        }
    }
}
