using System;
using System.Collections.Generic;

namespace Host.MPLSRelated
{ 

    public class Label
    {

        public short ID { get; set; }

        public byte TTL { get; set; }

        public Label()
        {
        }

        public Label(short id)
        {
            ID = id;
        }

        public static Label FromBytes(byte[] bytes)
        {
            Label label = new Label();

            label.ID = BitConverter.ToInt16(bytes);
            label.TTL = bytes[2];

            return label;
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(ID));
            bytes.Add(TTL);

            return bytes.ToArray();
        }

        public override bool Equals(object obj)
        {
            //tutaj tracimy informacje o typie oryginalnym obj
            var item = obj as Label;

            if (item == null)
                return false;
            //tutaj jest porownywany juz zrzutowany obiekt
            if (ReferenceEquals(this, item))
                return true;
            //wiec trzeba tego ifa
            if (obj.GetType() != GetType())
                return false;

            return ID.Equals(item.ID);
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

    }
}