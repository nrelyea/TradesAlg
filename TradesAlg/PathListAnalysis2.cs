using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TradesAlg
{
    public class PathListAnalysis2
    {
        public PathListAnalysis2() { }

        public List<OptionPackage> AllOptionPackages(List<Item> inventory, List<List<Trade>> pathList, string targetName, int targetAmount)
        {
            List<OptionPackage> optionPackageList = new List<OptionPackage>();

            for(int i = 0; i < pathList.Count; i++)
            {
                Console.WriteLine($"\nPath #{i+1}:");
                optionPackageList.Add(GenerateOptionPackage(inventory, pathList[i], targetName, targetAmount));
            }

            Console.WriteLine("\n");
            PrintOptionPackageList(optionPackageList);

            return optionPackageList;

        }

        private OptionPackage GenerateOptionPackage(List<Item> inventory, List<Trade> path, string targetName, int targetAmount)
        {
            Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName}...");

            List<string> sourceItemNames = SourceItemsFromInventoryDict(inventory);

            PLA2_Node baseNode = new PLA2_Node(targetName, null, path, sourceItemNames);

            //Console.WriteLine("\nNode structuring complete. Beginning traversal / simulation...");



            OptionPackage optionPackage = TraverseAndSimulateForOptionPackage(path, baseNode, targetAmount);



            return optionPackage;
        }

        private OptionPackage TraverseAndSimulateForOptionPackage(List<Trade> path, PLA2_Node baseNode, int targetAmount)
        {
            Dictionary<string, int> activeInventory = new Dictionary<string, int>();
            Dictionary<string, int> runningCost = new Dictionary<string, int>();
            Dictionary<Trade, int> tradeCounts = new Dictionary<Trade, int>();

            PLA2_Node node = baseNode;

            while (node != null)
            {
                // Node is source
                if (node.VitalTrade == null)
                {
                    // determine amount of source item needed to satisfy child node's vital trade
                    int costItemQuantityNeededForTrade = QuantityOfCostItemNeededForTrade(node.ChildNode.VitalTrade, node.ItemName);

                    // determine current active quantity of source item present
                    int activeItemQuantity = activeInventory.ContainsKey(node.ItemName) ? activeInventory[node.ItemName] : 0;

                    // determine how much to add to active inventory
                    int itemQuantityToAddForTrade = costItemQuantityNeededForTrade - activeItemQuantity;

                    // add necessary amount of source item to active inventory
                    activeInventory[node.ItemName] = itemQuantityToAddForTrade;
                    // add same amount to running cost total
                    runningCost[node.ItemName] = runningCost.ContainsKey(node.ItemName) ? runningCost[node.ItemName] + itemQuantityToAddForTrade : itemQuantityToAddForTrade;

                    node = node.ChildNode;
                    continue;
                }
                // Node is target
                else if (node.ChildNode == null)
                {
                    // if we have enough of the target item that we want, return the final upfront cost
                    if (activeInventory.ContainsKey(node.ItemName) && activeInventory[node.ItemName] >= targetAmount)
                    {
                        return new OptionPackage(runningCost, path, tradeCounts);     // FINAL UPFRONT COST RETURN
                    }

                    // if Vital Trade for target is possible, simulate the trade
                    if (SimulateTradeExecution(node.VitalTrade, ref activeInventory))
                    {
                        // update Trade Count for this trade as it has been executed
                        UpdateTradeCount(node.VitalTrade, ref tradeCounts);
                        
                        // after simulating the trade, analyze the target node again
                        continue;
                    }

                    // if Vital Trade for target is not possible...
                    foreach (PLA2_Node costNode in node.VitalTradeCostNodes)
                    {
                        int costItemQuantityNeededForTrade = QuantityOfCostItemNeededForTrade(node.VitalTrade, costNode.ItemName);

                        // if active inventory has none of costNode or not enough of it to satisfy the target's Vital Trade
                        if (!activeInventory.ContainsKey(costNode.ItemName) || activeInventory[costNode.ItemName] < costItemQuantityNeededForTrade)
                        {
                            // Analyze insufficient cost item's node
                            node = costNode;
                            continue;
                        }
                    }
                }
                // Node is mid-tree
                else
                {
                    // if Vital Trade for this node is possible, simulate the trade
                    if (SimulateTradeExecution(node.VitalTrade, ref activeInventory))
                    {
                        // update Trade Count for this trade as it has been executed
                        UpdateTradeCount(node.VitalTrade, ref tradeCounts);

                        // after simulating the trade, analyze its child node
                        node = node.ChildNode;
                        continue;
                    }

                    // if Vital Trade for this node is not possible...
                    foreach (PLA2_Node costNode in node.VitalTradeCostNodes)
                    {
                        int costItemQuantityNeededForTrade = QuantityOfCostItemNeededForTrade(node.VitalTrade, costNode.ItemName);

                        // if active inventory has none of costNode or not enough of it to satisfy this nodes's Vital Trade
                        if (!activeInventory.ContainsKey(costNode.ItemName) || activeInventory[costNode.ItemName] < costItemQuantityNeededForTrade)
                        {
                            // Analyze insufficient cost item's node
                            node = costNode;
                            continue;
                        }
                    }
                }
            }







            Console.WriteLine("%%% ERROR: Terminating Traversal, correct traversal path / condition not found %%%");
            
            return null;
        }

        private void UpdateTradeCount(Trade trade, ref Dictionary<Trade, int> counts)
        {
            if (counts.ContainsKey(trade))
            {
                //Console.WriteLine($"Changing count for Trade: [{trade.StringSummary()}] from {counts[trade]} to {counts[trade] + 1}");
                counts[trade] = counts[trade] + 1;
            }
            else
            {
                //Console.WriteLine($"Adding new trade execution for: [{trade.StringSummary()}]");
                counts[trade] = 1;
            }
        }

        private int QuantityOfCostItemNeededForTrade(Trade trade, string costItemName)
        {
            foreach (Item costItem in trade.CostItems)
            {
                if (costItem.Name == costItemName)
                {
                    return costItem.Quantity;
                }
            }

            // if somehow searching for costItem quanitity not needed for trade, return -1
            return -1;
        }


        private bool SimulateTradeExecution(Trade trade, ref Dictionary<string, int> activeInventory)
        {
            // Determine if trade is possible
            foreach (Item costItem in trade.CostItems)
            {
                // Immediately return false if active inventory does not contain enough of any cost item needed for trade
                if(!activeInventory.ContainsKey(costItem.Name) || activeInventory[costItem.Name] < costItem.Quantity)
                {
                    return false;
                }
            }

            // Enough of each cost item are present, so we can move forward with simulating the trade execution

            // Simulate trade execution
            foreach(Item costItem in trade.CostItems)
            {
                activeInventory[costItem.Name] -= costItem.Quantity;
            }
            foreach(Item resultItem in trade.ResultItems)
            {
                activeInventory[resultItem.Name] = activeInventory.ContainsKey(resultItem.Name) ? activeInventory[resultItem.Name] + resultItem.Quantity : resultItem.Quantity;
            }

            // trade has been verified possible and has been simulated, active inventory is up to date
            return true;
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

        private void PrintOptionPackageList(List<OptionPackage> optionPackageList)
        {
            for(int i = 0; i < optionPackageList.Count; i++)
            {
                Console.WriteLine($"Total upfront cost for Option #{i+1}:");
                foreach (KeyValuePair<string,int> costPair in optionPackageList[i].UpfrontCost)
                {
                    Console.WriteLine($" - {costPair.Value} {costPair.Key}");
                }
                optionPackageList[i].PrintOptionSummary();
            }
        }

        
    }
}
