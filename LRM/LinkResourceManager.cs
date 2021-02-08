using System;
using System.Collections.Generic;
using System.Linq;

namespace LRM
{
    class LinkResourceManager
    {
        public List<Link> Links { get; set; }

        public LinkResourceManager()
        {
            Links = new List<Link>();
            LoadLinks();
        }

        public void LoadLinks()
        {
            foreach (var row in Config.LRMROWS)
            {
                Links.Add(new Link(row.Split(" ")[0], row.Split(" ")[1]));
                Links.Add(new Link(row.Split(" ")[1], row.Split(" ")[0]));
            }
        }

        public Tuple<double, double, int, int> ChooseLinks(int number, string s)
        {
            string[] nodes = s.Split("&");
            List<Link> tempArray = new List<Link>();
            double f1 = 0.0;
            double f2 = 0.0;
            int FirstIndex = 0;
            int LastIndex = 0;
            List<int> rangeMin = new List<int>();
            List<int> rangeMax = new List<int>();

            for (int i = 0; i < nodes.Length; i++)
            {
                Link result = Links.FirstOrDefault(link => (link.SrcNode.Equals(nodes[i]) && link.DstNode.Equals(nodes[i + 1])));
                tempArray.Add(result);
                result = Links.FirstOrDefault(link => (link.SrcNode.Equals(nodes[i + 1]) && link.DstNode.Equals(nodes[i])));
                tempArray.Add(result);
                if (i == nodes.Length - 2)
                    break;
            }


            foreach (Link x in tempArray)
            {
                rangeMin.Add(x.GetRange().Item1);
                rangeMax.Add(x.GetRange().Item2);
            }

            if (rangeMin.Min() - number >= 0)
            {
                foreach (Link link in tempArray)
                {
                    (double f1Prim, double f2Prim, int idx1, int idx2) = link.AllocateSlots(new int[] { number });
                    f1 = f1Prim;
                    f2 = f2Prim;
                    FirstIndex = idx1;
                    LastIndex = idx2;
                }
            }
            else
            {
                foreach (Link link in tempArray)
                {
                    (double f1Prim, double f2Prim, int idx1, int idx2) = link.AllocateSlots(new int[] { number, rangeMin.Min(), rangeMax.Max() });
                    f1 = f1Prim;
                    f2 = f2Prim;
                    FirstIndex = idx1;
                    LastIndex = idx2;
                }
            }
            return Tuple.Create(f1, f2, FirstIndex, LastIndex);
        }

        public void ReleaseResources(string f1, string f2, string src, string dst)
        {
            Link link = Links.First(x => x.SrcNode.Equals(src) && x.DstNode.Equals(dst));
            int index = Links.IndexOf(link);
            Links[index].Release(Convert.ToDouble(f1), Convert.ToDouble(f2));
            link = Links.First(x => x.SrcNode.Equals(dst) && x.DstNode.Equals(src));
            index = Links.IndexOf(link);
            Links[index].Release(Convert.ToDouble(f1), Convert.ToDouble(f2));
        }

        public void AddLog(object s, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            if (s is string)
                Console.WriteLine($"[{DateTime.Now.ToString("H:mm:ss:ff")}]; {s}");
            Console.ResetColor();
        }

        //Taking slots from the end
        public Tuple<double, double, int, int> GoBackwards(int number)
        {
            int length = Links[0].FrequencyGrid.Count;

            double f2 = Links[0].FrequencyGrid[length - 1].End;
            int idx2 = Links[0].FrequencyGrid[length - 1].Index;

            double f1 = Links[0].FrequencyGrid[length - 1 - number].Begin;
            int idx1 = Links[0].FrequencyGrid[length - 1 - number].Index;

            return Tuple.Create(Math.Round(f1, 5), Math.Round(f2, 5), idx1, idx2);

        }

        //Allocating slots with already fixed band
        public void UseFixedBand(string f1, string f2, string s)
        {
            string[] nodes = s.Split("&");
            List<Link> tempArray = new List<Link>();

            for (int i = 0; i < nodes.Length; i++)
            {
                Link result = Links.FirstOrDefault(link => (link.SrcNode.Equals(nodes[i]) && link.DstNode.Equals(nodes[i + 1])));
                tempArray.Add(result);
                result = Links.FirstOrDefault(link => (link.SrcNode.Equals(nodes[i + 1]) && link.DstNode.Equals(nodes[i])));
                tempArray.Add(result);
                if (i == nodes.Length - 2)
                    break;
            }

            foreach (Link link in tempArray)
            {
                link.AllocateOnSpecificFreq(Math.Round(Convert.ToDouble(f1), 4), Math.Round(Convert.ToDouble(f2), 4));
            }
        }
    }
}
