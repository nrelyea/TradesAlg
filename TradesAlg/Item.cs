using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class Item
    {
        public string Name;
        public double Quantity;

        public Item(string name, double quantity)
        {
            Name = name;
            Quantity = quantity;
        }

        public override bool Equals(object obj)
        {
            if (obj is Item other)
            {
                return Name == other.Name && Quantity == other.Quantity;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Quantity);
        }

    }
}
