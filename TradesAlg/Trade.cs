using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class Trade
    {
        public string Category;
        public List<Item> CostItems;
        public List<Item> ResultItems;

        public Trade(string category, List<Item> costItems, List<Item> resultItems)
        {
            Category = category;
            CostItems = costItems;
            ResultItems = resultItems;           
        }

        public string StringSummary()
        {                       
            List<string> costList = new List<string>();
            List<string> resultList = new List<string>();

            foreach (Item item in CostItems)
            {
                costList.Add(item.Name);
            }
            foreach (Item item in ResultItems)
            {
                resultList.Add(item.Name);
            }

            return $"{Category} {string.Join(", ", costList)} for {string.Join(", ", resultList)}";

        }

        public bool IsPossible(List<Item> inventory)
        {
            foreach (Item costItem in CostItems)
            {
                string costItemName = costItem.Name;
                int costItemQuantity = costItem.Quantity;

                bool isPossible = false;
                foreach(Item item in inventory)
                {
                    if (item.Name == costItemName && item.Quantity >= costItemQuantity)
                    {
                        isPossible = true;
                    }
                }

                if (!isPossible) return false;
            }

            return true; // all required items & their quantities are available
        }

        public override bool Equals(object obj)
        {
            if (obj is Trade other)
            {
                return Category == other.Category &&
                       CostItems.SequenceEqual(other.CostItems) &&
                       ResultItems.SequenceEqual(other.ResultItems);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = Category.GetHashCode();
            foreach (var item in CostItems)
            {
                hash = HashCode.Combine(hash, item.GetHashCode());
            }
            foreach (var item in ResultItems)
            {
                hash = HashCode.Combine(hash, item.GetHashCode());
            }
            return hash;
        }
    }
}
