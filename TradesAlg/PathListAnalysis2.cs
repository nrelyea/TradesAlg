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

        public List<List<Item>> AllUpfrontCosts(List<Item> inventory, List<List<Trade>> pathList, string targetName, int targetAmount)
        {
            List<List<Item>> upfrontCostList = new List<List<Item>>();

            for(int i = 0; i < pathList.Count; i++)
            {
                Console.WriteLine($"\nPath #{i+1}:");
                upfrontCostList.Add(UpfrontCost(inventory, pathList[i], targetName, targetAmount));
            }

            PrintUpfrontCostList(upfrontCostList);

            return upfrontCostList;

        }

        private List<Item> UpfrontCost(List<Item> inventory, List<Trade> path, string targetName, int targetAmount)
        {
            Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName}...");

            Dictionary<string, int> activeInventory = new Dictionary<string, int>();
            Dictionary<string, int> runningCost = new Dictionary<string, int>();

            List<string> sourceItemNames = SourceItemsFromInventoryDict(inventory);

            PLA2_Node baseNode = new PLA2_Node(targetName, null, path, sourceItemNames);



            runningCost = TraverseAndSimulateForCost(baseNode, activeInventory, runningCost, targetName, targetAmount);



            return new List<Item>();
        }

        private Dictionary<string, int> TraverseAndSimulateForCost(PLA2_Node node, Dictionary<string, int> activeInventory, Dictionary<string, int> runningCost, string targetName, int targetAmount)
        {
            
            
            
            
            
            
            
            
            return runningCost;
        }

        // from the inventory Dict, generate a list of "source" items whose amounts present will be the basis of the path's upfront cost
        private List<string> SourceItemsFromInventoryDict(List<Item> inventory)
        {
            List<string> sourceItems = new List<string>();
            foreach(Item item in inventory)
            {
                if (item.Quantity > 0)
                {
                    sourceItems.Add(item.Name);
                    Console.WriteLine($"Source item: {item.Name} ({item.Quantity})");
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
