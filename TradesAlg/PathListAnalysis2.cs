using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class PathListAnalysis2
    {
        public PathListAnalysis2() { }

        public List<Dictionary<string, int>> AllUpfrontCosts(Dictionary<string, int> inventoryDict, List<List<JObject>> pathList, string targetName, int targetAmount)
        {
            List<Dictionary<string, int>> upfrontCostList = new List<Dictionary<string, int>>();

            foreach (List<JObject> path in pathList)
            {
                upfrontCostList.Add(UpfrontCost(inventoryDict, path, targetName, targetAmount));
            }

            PrintUpfrontCostList(upfrontCostList);

            return upfrontCostList;

        }

        private Dictionary<string, int> UpfrontCost(Dictionary<string, int> inventoryDict, List<JObject> path, string targetName, int targetAmount)
        {
            Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName}...");

            Dictionary<string, int> upfrontCost = new Dictionary<string, int>();

            Dictionary<string, int> activeInventory = new Dictionary<string, int>();

            List<string> sourceItems = SourceItemsFromInventoryDict(inventoryDict);

            PLA2_Node baseNode = new PLA2_Node(targetName, null, path, sourceItems);




            return upfrontCost;
        }

        private Dictionary<string, int> Cost(List<JObject> path, string targetName, int targetAmount)
        {
            return null;
        }

        // from the inventory Dict, generate a list of "source" items whose amounts present will be the basis of the path's upfront cost
        private List<string> SourceItemsFromInventoryDict(Dictionary<string, int> inventoryDict)
        {
            List<string> sourceItems = new List<string>();
            foreach(KeyValuePair<string,int> kvp in inventoryDict)
            {
                if (kvp.Value > 0)
                {
                    sourceItems.Add(kvp.Key);
                    Console.WriteLine($"Source item: {kvp.Key}");
                }
            }
            return sourceItems;
        }

        private void PrintUpfrontCostList(List<Dictionary<string, int>> upfrontCostList)
        {
            foreach (Dictionary<string, int> upfrontCost in upfrontCostList)
            {
                Console.WriteLine("Total upfront cost for this path:");
                foreach (KeyValuePair<string, int> costPair in upfrontCost)
                {
                    Console.WriteLine($" - {upfrontCost[costPair.Key]} {costPair.Key}");
                }
            }
        }

        
    }
}
