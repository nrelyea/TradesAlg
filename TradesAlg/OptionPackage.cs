using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class OptionPackage
    {
        public Dictionary<string, int> UpfrontCost;
        public List<Trade> Path;
        public Dictionary<Trade, int> TradeCounts;
         

        public OptionPackage(Dictionary<string, int> upfrontCost, List<Trade> path, Dictionary<Trade, int> tradeCounts)
        {
            UpfrontCost = upfrontCost;
            Path = path;
            TradeCounts = tradeCounts;
        }

        public void PrintOptionSummary()
        {
            for(int i = 0; i < Path.Count; i++)
            {
                //Console.WriteLine($"Step {i+1}: do the following trade {TradeCounts[Path[i]]} times: {Path[i].StringSummary()}");
                Console.WriteLine($"Step {i + 1}: {Path[i].AdvancedStringSummary(TradeCounts[Path[i]])}");
            }
        }
    }
}
