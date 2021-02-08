using System;
using System.Collections.Generic;
using System.Text;
using Commons.Tools;

namespace NetworkNodes
{
    public class RouteTable
    {
        public List<RouteTableRow> Rows { get; set; }

        public RouteTable()
        {
            Rows = new List<RouteTableRow>();
        }
    }
}
