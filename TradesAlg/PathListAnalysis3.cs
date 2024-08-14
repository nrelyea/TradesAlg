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
    public class PathListAnalysis3
    {
        public PathListAnalysis3() { }

        public List<OptionPackage> AllOptionPackages(List<Item> inventory, List<List<Trade>> pathList, string targetName, int targetAmount)
        {
            List<OptionPackage> optionPackageList = new List<OptionPackage>();

            for(int i = 0; i < pathList.Count; i++)
            {
                Console.WriteLine($"\nPath #{i+1}:");
                optionPackageList.Add(GenerateOptionPackage(inventory, pathList[i], targetName, targetAmount));
            }

            return optionPackageList;

        }

        private OptionPackage GenerateOptionPackage(List<Item> inventory, List<Trade> path, string targetName, int targetAmount)
        {
            Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName}...");

            List<string> sourceItemNames = SourceItemsFromInventoryDict(inventory);

            










            OptionPackage optionPackage = TraverseAndSimulateForOptionPackage(path, targetName, targetAmount);



            return optionPackage;
        }

        private OptionPackage TraverseAndSimulateForOptionPackage(List<Trade> path, string targetName, int targetAmount)
        {
            //Console.WriteLine($"Analyzing path with length {path.Count}");
            
            Dictionary<string, int> activeInventory = new Dictionary<string, int>();
            Dictionary<string, int> runningCost = new Dictionary<string, int>();
            Dictionary<Trade, int> tradeCounts = new Dictionary<Trade, int>();

            // generated a list of integer sets, each representing that corresponding Trade (in path)'s Vital Trade indices in the path
            List<Dictionary<string,int>> listOfCorrespondingIndicesForVitalTrades = GenerateListOfCorrespondingIndicesForVitalTrades(path, targetName, targetAmount);
            
            for(int i = 0; i < path.Count; i++)
            {
                Console.WriteLine($"Trade [{i}] ({path[i].StringSummary()})  Vital trade indices: {string.Join(", ", listOfCorrespondingIndicesForVitalTrades[i].Values)}");
            }



            int lastIndex = path.Count - 1;
            int currentTradeEvalIndex = lastIndex;

            while(true)
            {
                break;
                // attempt to execute this trade
                  // if successful
                    // set currentTradeEvalIndex to lastIndex
                    // continue;
                  // if not successful
                    // foreach cost Item in this trade
                        // if not enough of this cost item is available to perform the trade
                            // if cost Item is a source
                                // add enough of source item to satisfy the trade
                                // update active inventory
                                // break;
                            // if cost Item is not a source
                                // set currentTradeEvalIndex to the vital Trade index of this costItem
                                // break;

            }










            Console.WriteLine("%%% ERROR: Terminating Traversal, correct traversal path / condition not found %%%");
            
            return null;
        }

        private List<Dictionary<string, int>> GenerateListOfCorrespondingIndicesForVitalTrades(List<Trade> path, string targetName, int targetAmount)
        {
            List<Dictionary<string, int>> listOfCorrespondingIndicesForVitalTrades = new List<Dictionary<string, int>>();
            listOfCorrespondingIndicesForVitalTrades.Add(new Dictionary<string, int>());   // add empty set that will correspond to first Trade in path

            for(int i = 1; i < path.Count; i++)
            {
                Dictionary<string, int> vitalTradeIndicesForThisTrade = new Dictionary<string, int>();

                foreach(Item costItem in path[i].CostItems)
                {                                       
                    bool costItemVitalTradefound = false;
                    for (int j = i - 1; j >= 0 && !costItemVitalTradefound; j--)
                    {
                        foreach(Item resultItem in path[j].ResultItems)
                        {
                            if(resultItem.Name == costItem.Name)
                            {
                                Console.WriteLine($"Marking index {j} as a Vital Trade for trade at index {i} ({path[i].StringSummary()})");
                                vitalTradeIndicesForThisTrade.Add(costItem.Name,j);
                                costItemVitalTradefound = true;
                                break;
                            }
                        }
                    }
                }

                listOfCorrespondingIndicesForVitalTrades.Add(vitalTradeIndicesForThisTrade);
            }



            return listOfCorrespondingIndicesForVitalTrades;
        }

        private void UpdateTradeCount(Trade trade, ref Dictionary<Trade, int> counts)
        {
            if (counts.ContainsKey(trade))
            {
                Console.WriteLine($"Changing count for Trade: [{trade.StringSummary()}] from {counts[trade]} to {counts[trade] + 1}");
                counts[trade] = counts[trade] + 1;
            }
            else
            {
                Console.WriteLine($"Adding new trade execution for: [{trade.StringSummary()}]");
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

        public void PrintOptionPackageList(List<OptionPackage> optionPackageList)
        {
            Console.WriteLine("\n");
            for(int i = 0; i < optionPackageList.Count; i++)
            {
                Console.WriteLine($"\nTotal upfront cost for Option #{i+1}:");
                foreach (KeyValuePair<string,int> costPair in optionPackageList[i].UpfrontCost)
                {
                    Console.WriteLine($" - {costPair.Value} {costPair.Key}");
                }
                optionPackageList[i].PrintOptionSummary();
            }
        }

        
    }
}
