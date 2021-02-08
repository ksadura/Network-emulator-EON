using System;
using System.Data;
using System.IO;

namespace CableCloud
{
    class RoutingTable
    {
        private static DataTable table;
        private DataColumn[] columns = new DataColumn[6];
        private DataRow row;
        

        public RoutingTable()
        {
            table = new DataTable();
            UploadTable();
            
        }
        public void UploadTable()
        {
            //Columns' names
            string[] names = ConfigCloud.COLUMN_NAMES.Split(" ");
            
            int i = 0;
            foreach(string name in names)
            {
                columns[i] = new DataColumn();
                columns[i].ColumnName = name;
                i += 1;
            }
            table.Columns.AddRange(columns);

            foreach(var rows in ConfigCloud.ROWS)
            {
                row = table.NewRow();
                string[] values = rows.Split(" ");
                for(int k = 0; k < values.Length; k++)
                {
                    row[names[k]] = values[k];
                }
                table.Rows.Add(row);
            }
        }

        public string GetNextHop(string nodeIN, string portIN)
        {
            for (int i = 0; i < ConfigCloud.ROWS.Count; i++)
            {
                if(table.Rows[i]["PORT_IN"].ToString() == portIN && table.Rows[i]["R_IN"].ToString() == nodeIN)
                {
                    if (table.Rows[i]["WORKING"].ToString() == "1")
                        return table.Rows[i]["R_OUT"].ToString();
                }
            }
            return "DISCARD";
        }
        public string GetNextPort(string nodeIN, string portIN)
        {
            for(int i = 0; i < ConfigCloud.ROWS.Count; i++)
            {
                if (table.Rows[i]["PORT_IN"].Equals(portIN) && table.Rows[i]["R_IN"].Equals(nodeIN))
                {
                    if (table.Rows[i]["WORKING"].ToString() == "1")
                        return table.Rows[i]["PORT_OUT"].ToString();
                }
            }
            return "DISCARD";
        }

        //Checking if packet's size fits into the link
        public int CheckCapacity(string nodeIN, string portIN)
        {
            int rowNumber = 0;
            for (int i = 0; i < ConfigCloud.ROWS.Count; i++)
            {
                if (table.Rows[i]["PORT_IN"].Equals(portIN) && table.Rows[i]["R_IN"].Equals(nodeIN))
                {
                    rowNumber = i;
                    break;
                }
            }
            return Convert.ToInt32(table.Rows[rowNumber]["CAPACITY"]);
        }

        //Destroying or restoring a connection in the result of router shutdown
        public static void HandleAdjacency(string nodeName, int option)
        {
            for (int i = 0; i< ConfigCloud.ROWS.Count; i++)
            {
                if (table.Rows[i]["R_IN"].ToString().Equals(nodeName) || table.Rows[i]["R_OUT"].ToString().Equals(nodeName))
                {
                    table.Rows[i]["WORKING"] = option.ToString();
                }
            }
        }

        //Spoiling connection between two routers
        public static void SpoilOrRestoreLink(string node1, string node2, int number)
        {
            int c = 0;
            int j = 0;
            for (int i = 0; i < ConfigCloud.ROWS.Count; i++)
            {
                if ((table.Rows[i]["R_IN"].ToString().Equals(node1) && table.Rows[i]["R_OUT"].ToString().Equals(node2)) || (table.Rows[i]["R_IN"].ToString().Equals(node2) && table.Rows[i]["R_OUT"].ToString().Equals(node1)))
                {
                    if (table.Rows[i]["WORKING"].ToString() == number.ToString())
                    {
                        c++;
                        if (c == 2)
                        {
                            Console.WriteLine("Nothing's changed");
                            return;
                        }
                    }
                    else
                    {
                        j++;
                        table.Rows[i]["WORKING"] = number.ToString();
                        if(j == 2)
                        {
                            AddLog((number == 0) ? "Fiber is being spoilt" : "Fiber is being fixed", ConsoleColor.DarkYellow);
                            return;
                        }
                    }
                }
            }
            Console.WriteLine("These two routers are not directly connected");
        }

        //Logging
        public static void AddLog(string log, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond.ToString().PadLeft(3, '0')}] {log}");
            Console.ResetColor();
        }


    }
}
