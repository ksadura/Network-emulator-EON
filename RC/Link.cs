using System;
using System.Collections.Generic;
using System.Text;

namespace Dijkstra
{
    class Link
    {
        public int Capacity { get; set; }
        public int Weight { get; set; }
        public Node Source { get; set; }
        public Node Destination {get; set;}

        //Constructor
        public Link (int _capacity, int _weight, string id)
        {
            Capacity = _capacity;
            Weight = _weight;
            Source = new Node(id.Split("-")[0]);
            Destination = new Node(id.Split("-")[1]);
        }

    }
}
