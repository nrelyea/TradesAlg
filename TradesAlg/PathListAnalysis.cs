using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class PathListAnalysis
    {
        public PathListAnalysis() { }

        public List<Dictionary<string, int>> UpfrontCosts(List<List<JObject>> pathList, string targetName, int targetAmount)
        {
            List<Dictionary<string, int>> upfrontCostList = new List<Dictionary<string, int>>();

            foreach (List<JObject> path in pathList)
            {
                upfrontCostList.Add(Cost(path, targetName, targetAmount));
            }

            PrintUpfrontCostList(upfrontCostList);

            return upfrontCostList;

        }

        private Dictionary<string, int> Cost(List<JObject> path, string targetName, int targetAmount)
        {
            Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName}...");

            Dictionary<string, int> upfrontCost = new Dictionary<string, int>();

            int tradeYield;  // minimum amount of target item yielded by the trade (given by trade declaration / description)
            JObject tradeNeeded = TradeNeeded(path, targetName, out tradeYield);

            // if tradeNeeded is null (no further trade steps are needed / this is the original item in this branch), then return the amount of that initial starting item
            if(tradeNeeded == null)
            {
                Console.WriteLine($"No trade yielding {targetName}... returning cost dict for {targetAmount} {targetName}");
                upfrontCost.Add(targetName, targetAmount);
                return upfrontCost;
            }

            Console.WriteLine($"Trade found that yields {tradeYield} {targetName}");

            // get the number of times the trade must be made to get the desired quantity of the target item
            int numTradesRequired = NumTradesRequired(targetAmount, tradeYield);
            Console.WriteLine($"Will need {numTradesRequired} of this trade to get {targetAmount} {targetName}");

            foreach(var costItem in tradeNeeded["cost"])
            {
                string costItemName = costItem.Value<string>("name");
                int costItemQuantity = costItem.Value<int>("quantity");

                // recurssively call Cost() to determine the cost of each cost item of the trade in question
                Dictionary<string, int> subCost = Cost(path, costItemName, costItemQuantity * numTradesRequired);   // multiplying the costItemQuantity by how many trades needed

                // combine each subCost dict with the running upfrontCost dict, adding new cost items to the dict, and combining quantities of existing ones
                CombineCostDicts(subCost, upfrontCost);
            }

            return upfrontCost;
        }
        private void PrintUpfrontCostList(List<Dictionary<string, int>> upfrontCostList)
        {
            foreach(Dictionary<string, int> upfrontCost in upfrontCostList)
            {
                Console.WriteLine("Total upfront cost for this path:");
                foreach (KeyValuePair<string, int> costPair in upfrontCost)
                {
                    Console.WriteLine($" - {upfrontCost[costPair.Key]} {costPair.Key}");
                }
            }
        }

        private void CombineCostDicts(Dictionary<string, int> subCost, Dictionary<string, int> upfrontCost)
        {
            foreach (KeyValuePair<string, int> subCostPair in subCost)
            {
                if(upfrontCost.ContainsKey(subCostPair.Key))
                {
                    // if cost item being added to running upfront cost already in the cost dict, add the value to that key
                    Console.WriteLine($"Adding {subCost[subCostPair.Key]} to existing {upfrontCost[subCostPair.Key]} of {subCostPair.Key} in the upfront cost dict");
                    upfrontCost[subCostPair.Key] = upfrontCost[subCostPair.Key] + subCost[subCostPair.Key];
                }
                else
                {
                    // otherwise (cost item is new), simply add it to the running upfront cost dict
                    Console.WriteLine($"Adding new amount of {subCost[subCostPair.Key]} {subCostPair.Key} to the upfront cost dict");
                    upfrontCost[subCostPair.Key] = subCost[subCostPair.Key];
                }
            }
        }

        private int NumTradesRequired(int targetAmount, int tradeYield)
        {
            if (targetAmount > tradeYield)
            {
                int quotient = targetAmount / tradeYield;

                if (targetAmount % tradeYield != 0)
                {
                    quotient++;
                }

                return quotient;
            }
            else
            {
                return 1;
            }
        }

        private JObject TradeNeeded(List<JObject> path, string targetName, out int tradeYield)
        {
            foreach (JObject trade in path)   // search through path to find trade that that results in the current target item
            {
                foreach (var resultItem in trade["result"])
                {
                    if (resultItem.Value<string>("name") == targetName)
                    {
                        tradeYield = resultItem.Value<int>("quantity");
                        return trade;
                    }
                }
            }

            tradeYield = 0;
            return null;
        }
    }
}
