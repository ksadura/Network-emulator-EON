using System;
using System.Collections.Generic;
using System.Linq;

namespace Host.MPLSRelated
{
    public class LabelStack
    {

        public Stack<Label> stack_of_labels { get; set; }

        public LabelStack()
        {
            stack_of_labels = new Stack<Label>();
        }

        public byte[] ToBytes()
        {
            List<byte> list_of_bytes = new List<byte>();
            bool stack_flag_bit = new bool();
            if (IsEmpty())
                stack_flag_bit = false;
            else
                stack_flag_bit = true;

            list_of_bytes.AddRange(BitConverter.GetBytes(stack_flag_bit));

            List<Label> iteration_to_labels = stack_of_labels.ToList();

            for(int i=0; i < iteration_to_labels.Count; i++)
            {
                list_of_bytes.AddRange(iteration_to_labels[i].ToBytes());
                //dodawanie flagi czy to jest BoS(Bottom of the stack) true jesli jest
                if (i != iteration_to_labels.Count - 1)
                    list_of_bytes.AddRange(BitConverter.GetBytes(true));
                else
                    list_of_bytes.AddRange(BitConverter.GetBytes(false));

            }
            return list_of_bytes.ToArray();

        }
        public static LabelStack FromBytes(byte[] bytes)
        {
            LabelStack returned_obj = new LabelStack();
            //czy stack nie jest pusty
            bool stack_flag = BitConverter.ToBoolean(bytes);
            if (stack_flag)
            {
                bool no_BoS_flag = true;
                int i = 1;
                while (no_BoS_flag)
                {
                    Label temp = new Label { ID = BitConverter.ToInt16(bytes,i), TTL = bytes[i + 2] };
                    returned_obj.stack_of_labels.Push(temp);
                    bool is_BoS = BitConverter.ToBoolean(bytes, i + 3);
                    if (is_BoS)
                        break;
                    else
                        i = i + 4;
                }
            }
            return returned_obj;

        }
        public bool IsEmpty()
        {
            return stack_of_labels.Count == 0;
        }

        public int GetBytes_Length()
        {
            int bytes_length;
            if (IsEmpty()) bytes_length=1;
            else bytes_length=1 + stack_of_labels.Count * 4;
            //Bool czy stack jest pusty+ilość elementów w stacku*4(3 bajty label-ID 2+TTL 1+1 bajt Bool czy to jest BoS) 

            return bytes_length;
        }

    }
}