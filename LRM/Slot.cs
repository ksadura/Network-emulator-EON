using System;
using System.Collections.Generic;
using System.Text;

namespace LRM
{
    class Slot
    {
        public double Begin { set; get; }
        public double End { set; get; }
        public bool Allocated { set; get; }
        public int Index { set; get; }

        //slot's width in ITU-T standard
        private readonly double width;

        public Slot(double _begin, double _end, int _index)
        {
            Begin = _begin;
            End = _end;
            Allocated = false;
            Index = _index;
            width = 12.5;
        }
    }
}
