using System;
using System.Collections.Generic;
using System.Text;

namespace k1tool
{
    public class Multi
    {
        public string Name;
        public int Volume;

        public Multi(byte[] data)
        {
            int offset = 0;
            byte b = 0;  // will be reused when getting the next byte

            Name = GetName(data, offset);
            offset += Name.Length;

            (b, offset) = Util.GetNextByte(data, offset);
            Volume = b;

        }


        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Name);
            builder.Append("\n");
            builder.Append(String.Format("volume = {0}\n", Volume + 1));
            return builder.ToString();
        }

        private string GetName(byte[] data, int offset)
        {
            byte[] bytes = 
            { 
                data[offset], 
                data[offset + 1], 
                data[offset + 2], 
                data[offset + 3], 
                data[offset + 4], 
                data[offset + 5], 
                data[offset + 6], 
                data[offset + 7]
            };
            string name = Encoding.ASCII.GetString(bytes);
            return name;
        }

    }
}
