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

        public List<List<Item>> AllUpfrontCosts(Dictionary<string, int> inventoryDict, List<List<Trade>> pathList, string targetName, int targetAmount)
        {
            List<List<Item>> upfrontCostList = new List<List<Item>>();

            for(int i = 0; i < pathList.Count; i++)
            {
                Console.WriteLine($"\nPath #{i+1}:");
                upfrontCostList.Add(UpfrontCost(inventoryDict, pathList[i], targetName, targetAmount));
            }

            PrintUpfrontCostList(upfrontCostList);

            return upfrontCostList;

        }

        private List<Item> UpfrontCost(Dictionary<string, int> inventoryDict, List<Trade> path, string targetName, int targetAmount)
        {
            Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName}...");

            List<Item> upfrontCost = new List<Item>();

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

        private void PrintUpfrontCostList(List<List<Item>> upfrontCostList)
        {
            foreach (List<Item> upfrontCost in upfrontCostList)
            {
                Console.WriteLine("Total upfront cost for this path:");
                foreach (Item costPair in upfrontCost)
                {
                    Console.WriteLine($" - {costPair.Quantity} {costPair.Name}");
                }
            }
        }

        
    }
}
