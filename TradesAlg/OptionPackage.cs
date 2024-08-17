using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class OptionPackage
    {
        public Dictionary<string, double> UpfrontCost;
        public List<Trade> Path;
        public Dictionary<Trade, int> TradeCounts;
        public Dictionary<string, double> Remainder;
        public bool ContainsProbability;


        public OptionPackage(Dictionary<string, double> upfrontCost, List<Trade> path, Dictionary<Trade, int> tradeCounts, Dictionary<string, double> remainder, bool containsProbability)
        {
            UpfrontCost = upfrontCost;
            Path = path;
            TradeCounts = tradeCounts;
            Remainder = remainder;
            ContainsProbability = containsProbability;
        }

        public void PrintOptionSummary()
        {
            //Console.WriteLine($"Path length: {Path.Count}  TradeCounts #: {TradeCounts.Count}");
            
            for(int i = 0; i < Path.Count; i++)
            {
                Trade trade = Path[i];
                //Console.WriteLine($"Trade: {Path[i].StringSummary()}");
                Console.WriteLine($"Step {i + 1}: {Path[i].AdvancedStringSummary(TradeCounts[Path[i]])}");
            }

            List<string> remainderStringList = new List<string>();
            foreach(KeyValuePair<string, double> pair in Remainder)
            {
                if(pair.Value > 0) remainderStringList.Add($"{Math.Round(pair.Value,2)}x {pair.Key}");
            }

            string leftoverItems = String.Join(", ", remainderStringList.ToArray());
            leftoverItems = leftoverItems.Length > 0 ? leftoverItems : "N/A";

            Console.WriteLine($"Leftover items: {leftoverItems}");
        }

        public bool IsCheaperThan(OptionPackage otherOption)
        {
            foreach(KeyValuePair<string,double> cost in UpfrontCost)
            {
                if(otherOption.UpfrontCost.ContainsKey(cost.Key) && cost.Value < otherOption.UpfrontCost[cost.Key])
                {
                    //Console.WriteLine($"This option is cheaper than the other ({cost.Value} < {otherOption.UpfrontCost[cost.Key]})");
                    return true;
                }
            }
            return false;
        }
    }
}
