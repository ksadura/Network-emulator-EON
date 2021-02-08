using System;
using System.Collections.Generic;
using System.Linq;

namespace LRM
{
    class Link
    {
        public string SrcNode { set; get; }
        public string DstNode { set; get; }
        public List<Slot> FrequencyGrid { get; set; }

        public Link(string source, string destination)
        {
            SrcNode = source;
            DstNode = destination;
            FrequencyGrid = new List<Slot>();
            LoadGrid();
        }

        public Tuple<double, double, int, int> AllocateSlots(int[] numbers)
        {
            var freeSlots = new List<Slot>();
            if (numbers.Length == 1)
            {
                freeSlots = FrequencyGrid.Where(slot => slot.Allocated == false).ToList();
            }
            else
            {
                freeSlots = FrequencyGrid.Where(slot => slot.Index > numbers[2] && !slot.Allocated).ToList();
            }
            for (int i = 0; i < numbers[0]; ++i)
            {
                freeSlots[i].Allocated = true;
            }

            double firstFrequency = Math.Round(freeSlots[0].Begin, 4);
            double lastFrequency = Math.Round(freeSlots[numbers[0] - 1].End, 4);
            int firstIndex = freeSlots[0].Index;
            int lastIndex = freeSlots[numbers[0] - 1].Index;

            return Tuple.Create(firstFrequency, lastFrequency, firstIndex, lastIndex);

        }

        public void LoadGrid()
        {
            int i = 0;
            double begin = 192.95;
            double end = 193.25;

            while (Math.Round(begin, 4) != end)
            {
                FrequencyGrid.Add(new Slot(begin, begin + 0.0125, i));
                begin += 0.0125;
                i += 1;
            }
        }

        public int GetNoFreeSlots()
        {
            var result = FrequencyGrid.Where(slot => slot.Allocated == false).ToList();
            return result.Count;
        }

        public Tuple<int, int> GetRange()
        {
            List<int> Arr = new List<int>();
            var result = FrequencyGrid.Where(slot => slot.Allocated == true).ToList();
            if (result.Count == 0)
                return Tuple.Create(24, 0);
            foreach (Slot s in result)
            {
                Arr.Add(s.Index);
            }
            return Tuple.Create(Arr.Min(), Arr.Max());
        }

        public void Release(double begin, double end)
        {
            var takenSlots = FrequencyGrid.Where(x => (x.Begin >= begin && x.End <= end)).ToList();
            foreach (Slot s in takenSlots)
            {
                s.Allocated = false;
            }
        }

        public void AllocateOnSpecificFreq(double f1, double f2)
        {
            var freeSlots = new List<Slot>();

            freeSlots = FrequencyGrid.Where(slot => slot.Begin >= f1 && slot.End <= f2).ToList();
            freeSlots.ForEach(delegate (Slot slot)
            {
                slot.Allocated = true;
            });

        }
    }
}
