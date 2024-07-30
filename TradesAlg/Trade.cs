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
    }
}
