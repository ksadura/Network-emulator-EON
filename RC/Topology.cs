using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Dijkstra
{
    static class Extensions
    {
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key].Add(value);
            }
            else
            {
                dictionary.Add(key, new List<TValue> { value });
            }
        }
    }

    class Topology
    {
        //Array of network nodes
        private List<Node> nodes;
        //Dict of network links
        private List<Link> links;
        //Dictionary of all connections
        Dictionary<string, List<List<string>>> edges;
        public Topology()
        {
            nodes = Config.nodesArray;
            links = Config.linksArray;
            edges = new Dictionary<string, List<List<string>>>();
            CreateAdjacency();
        }

        private void CreateAdjacency()
        {
            foreach(Link link in links)
            {
                edges.AddOrUpdate(link.Source.NodeName, new List<string> { link.Destination.NodeName, link.Weight.ToString(), link.Capacity.ToString() });
                edges.AddOrUpdate(link.Destination.NodeName, new List<string> { link.Source.NodeName, link.Weight.ToString(), link.Capacity.ToString() });
            }
        }
        
        public Tuple<double, Dictionary<string, string>> Dijkstra(string src, string dst, int capacity)
        {
            Dictionary<string, List<List<string>>> _nodes = this.edges;
            var nodesLeft = new HashSet<string>(_nodes.Keys);
            var distance = new Dictionary<string, double>();
            var previous = new Dictionary<string, string>();
            var tempDict = new Dictionary<string, double>();
            foreach (string node in _nodes.Keys.ToList())
            {
                distance.Add(node, int.MaxValue);
                previous.Add(node, null);
            }
            distance[src] = 0.0;
            while (nodesLeft.Count != 0)
            {
                foreach (string nodeLeft in nodesLeft)
                {
                    if (distance.Keys.Contains(nodeLeft))
                    {
                        tempDict.Add(nodeLeft, distance.First(x => x.Key == nodeLeft).Value);
                    }
                }
                var closestNode = tempDict.OrderBy(k => k.Value).First().Key;
                tempDict.Clear();
                nodesLeft.Remove(closestNode);
                if (closestNode == dst)
                    return Tuple.Create(distance[dst], previous);
                var minKey = this.edges[closestNode];
                foreach (List<string> edge in minKey)
                {
                    string node = edge[0];
                    var weight = edge[1];
                    var newDistance = distance[closestNode] + double.Parse(weight);
                    if (newDistance < distance[node])
                    {
                        distance[node] = newDistance;
                        previous[node] = closestNode;
                    }
                }

            }
            return Tuple.Create(distance[dst], previous);
        }
        public List<string> FindPath(Dictionary<string, string> previousNodes, string dstNode)
        {
            var Array = new List<string>();
            while (dstNode != null)
            {
                Array.Add(dstNode);
                dstNode = previousNodes[dstNode];
            }
            Array.Reverse();
            return Array;
        }
        
        public void RemoveLink(string src, string dst)
        {
            foreach (List<string> arr in this.edges[src])
            {
                if (arr.Contains(dst))
                {
                    edges[src].Remove(arr);
                    break;
                }
            }
            foreach (List<string> arr in this.edges[dst])
            {
                if (arr.Contains(src))
                {
                    edges[dst].Remove(arr);
                    break;
                }
            }
        }

        //Include ports in the path
        public string ConcatenatePorts(List<string> path)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                string src = path[i];
                string dst = path[i + 1];
                var result = Config.linkWithPorts.First(x => x.Key == src + "-" + dst).Value;
                sb.Append(src + "-(" + result.Split("-")[0] + ") ("+result.Split("-")[1]+")-");
                if (i == path.Count - 2)
                    break;
            }

            sb.Append(path[path.Count - 1]);
            return sb.ToString();
        }
        //Updating occupied slots
        //public void UpdateCapacity(string src, string dst, int number)
        //{
        //    foreach (List<string> arr in this.edges[src])
        //    {
        //        if (arr.Contains(dst))
        //        {
        //            int index = edges[src].IndexOf(arr);
        //            int newValue = Convert.ToInt32(edges[src][index][2]) - number;
        //            edges[src][index][2] = newValue.ToString();
        //            break;
        //        }
        //    }
        //}
        //public void IncludeCapaciousLinks(ref Dictionary<string, List<List<string>>> dict, int number)
        //{
        //    foreach (var key in dict.Keys.ToList())
        //    {
        //        for (int i = dict[key].Count - 1; i >= 0; i--)
        //        {
        //            if (Convert.ToInt32(dict[key][i][2]) < number && key != "H1" && key != "H3" && key != "H2")
        //            {
        //                if(dict[key][i][0] != "H1" && dict[key][i][0] != "H3" && dict[key][i][0] != "H2")
        //                    dict[key].RemoveAt(i);
        //            }
        //        }
        //    }
        //}

    }
}
