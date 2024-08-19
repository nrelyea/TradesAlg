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
        public double UpfrontCostTotal;
        public Dictionary<string, double> Remainder;
        public double RemainderTotal;

        public List<Trade> Path;
        public Dictionary<Trade, int> TradeCounts;
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
            if(UpfrontCostTotal < otherOption.UpfrontCostTotal)
            {
                return true;
            }
            else if(UpfrontCostTotal > otherOption.UpfrontCostTotal)
            {
                return false;
            }

            // 1st Tiebreaker: if costs are equal, the larger remainder is considered cheaper option
            if (RemainderTotal > otherOption.RemainderTotal)
            {
                return true;
            }
            else if (RemainderTotal < otherOption.RemainderTotal)
            {
                return false;
            }

            // 2nd Tiebreaker: if remainders are equal, sort by optionpackage hash code
            return this.GetHashCode() < otherOption.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = Remainder.GetHashCode();
            foreach (Trade trade in Path)
            {
                hash = HashCode.Combine(hash, trade.GetHashCode());
            }
            return hash;
        }
    }
}
