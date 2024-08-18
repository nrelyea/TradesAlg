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
                if(Criteria.LoggingEnabled) Console.WriteLine($"\nPath #{i+1}:");
                if(Criteria.LoggingEnabled) PrintPath(pathList[i]);
                OptionPackage optionPackage = GenerateOptionPackage(inventory, pathList[i], targetName, targetAmount, Criteria.LoggingEnabled);
                if(optionPackage != null) optionPackageList.Add(optionPackage);
            }

            // Sort options by ascending cost (cheapest first)
            //optionPackageList.Sort((x, y) => x.IsCheaperThan(y) ? -1 : (y.IsCheaperThan(x) ? 1 : 0));
            optionPackageList.Sort((x, y) => x.IsCheaperThan(y) ? -1 : 1 );

            return optionPackageList;
        }

        private OptionPackage GenerateOptionPackage(List<Item> inventory, List<Trade> path, string targetName, int targetAmount, bool logging)
        {
            //Console.WriteLine($" --- Starting Cost analysis to procure {targetAmount} {targetName} ---");

            HashSet<string> sourceItemNames = SourceItemsFromInventoryDict(inventory);

            OptionPackage optionPackage = TraverseAndSimulateForOptionPackage(path, sourceItemNames, targetName, targetAmount, logging);

            if (IsOptionGarbage(optionPackage)) return null;

            return optionPackage;
        }

        private bool IsOptionGarbage(OptionPackage optionPackage)
        {
            if (optionPackage == null) return true;
            
            if (optionPackage.TradeCounts.Count != optionPackage.Path.Count) return true;

            return false;
        }

        private OptionPackage TraverseAndSimulateForOptionPackage(List<Trade> path, HashSet<string> sourceItemNames, string targetName, int targetAmount, bool logging)
        {            
            if (logging) Console.WriteLine($"Analyzing path with length {path.Count}");
            
            Dictionary<string, double> activeInventory = new Dictionary<string, double>();
            Dictionary<string, double> runningCost = new Dictionary<string, double>();
            Dictionary<Trade, int> tradeCounts = new Dictionary<Trade, int>();
            bool hasProbability = false;

            // generated a list of integer sets, each representing that corresponding Trade (in path)'s Vital Trade indices in the path
            List<Dictionary<string,int>> listOfCorrespondingIndicesForVitalTrades = GenerateListOfCorrespondingIndicesForVitalTrades(path, targetName, targetAmount, logging);

            if (VitalTradeIndicesIndicateGarbagePath(listOfCorrespondingIndicesForVitalTrades, logging)) return null;

            for(int i = 0; i < path.Count; i++)
            {
                if (logging) Console.WriteLine($"Trade [{i}] ({path[i].StringSummary()})  Vital trade indices: {string.Join(", ", listOfCorrespondingIndicesForVitalTrades[i].Values)}");
            }
            if (logging) Console.WriteLine("--- STARTING TRAVERSAL / SIMULATION NOW ---");


            int lastIndex = path.Count - 1;
            int currentTradeEvalIndex = lastIndex;

            while(true)
            {
                Trade currentTrade = path[currentTradeEvalIndex];

                // if we have enough of the target item that we want, return the final upfront cost
                if (activeInventory.ContainsKey(targetName) && activeInventory[targetName] >= targetAmount)
                {
                    activeInventory[targetName] = 0;     // Zero out target item quantity in inventory, which is being loaded into OptionPackage as remainder

                    return new OptionPackage(runningCost, path, tradeCounts, activeInventory, hasProbability);     // FINAL UPFRONT COST RETURN
                }

                // if Trade target is possible, simulate the trade
                if (SimulateTradeExecution(currentTrade, ref activeInventory))
                {                   
                    // update Trade Count for this trade as it has been executed
                    UpdateTradeCount(currentTrade, logging, ref tradeCounts);
                    if (logging) PrintActiveInventory(activeInventory);

                    // if trade added contains probabilty, update this flag
                    if(TradeContainsProbability(currentTrade)) hasProbability = true;

                    // after simulating the trade, analyze the final trade again
                    currentTradeEvalIndex = lastIndex;
                    continue;
                }
                else // if Trade is not possible
                {
                    if(logging) Console.WriteLine($"Trade [{currentTrade.StringSummary()}] not possible");
                    foreach(Item costItem in currentTrade.CostItems)
                    {
                        // if we don't have enough of this cost item to execute the trade
                        if(!activeInventory.ContainsKey(costItem.Name) || activeInventory[costItem.Name] < costItem.Quantity)
                        {
                            if (logging) Console.WriteLine($"Need more {costItem.Name} to complete this trade");
                            // if costItem is a source
                            if (sourceItemNames.Contains(costItem.Name))
                            {
                                double itemQuantityToAddForTrade = activeInventory.ContainsKey(costItem.Name) ? costItem.Quantity - activeInventory[costItem.Name] : costItem.Quantity;

                                // add necessary amount to running cost total
                                if (logging) Console.WriteLine($"Added {itemQuantityToAddForTrade}x {costItem.Name} to make trade possible");
                                runningCost[costItem.Name] = runningCost.ContainsKey(costItem.Name) ? runningCost[costItem.Name] + itemQuantityToAddForTrade : itemQuantityToAddForTrade;

                                // update amount of source item in active inventory
                                activeInventory[costItem.Name] = costItem.Quantity;
                                if (logging) PrintActiveInventory(activeInventory);

                                // go back to analyzing final trade
                                currentTradeEvalIndex = lastIndex;
                                break;
                                
                            }
                            else // if costItem is NOT a source
                            {
                                // Retrieve the index of the trade that is the vital trade for this cost item, and go analyze that trade
                                currentTradeEvalIndex = listOfCorrespondingIndicesForVitalTrades[currentTradeEvalIndex][costItem.Name];
                                break;
                            }
                        }
                    }
                }

         

            }

            Console.WriteLine("%%% ERROR: Terminating Traversal, correct traversal path / condition not found %%%");
            
            return null;
        }

        private bool VitalTradeIndicesIndicateGarbagePath(List<Dictionary<string, int>> listOfCorrespondingIndicesForVitalTrades, bool logging)
        {          
            HashSet<int> vitalTradeIndicesNoted = new HashSet<int>();
            foreach(Dictionary<string, int> indices in listOfCorrespondingIndicesForVitalTrades)
            {
                foreach(KeyValuePair<string, int> kvp in indices)
                {
                    vitalTradeIndicesNoted.Add(kvp.Value);
                }
            }

            for(int i = 0; i < listOfCorrespondingIndicesForVitalTrades.Count - 1; i++)
            {
                if (!vitalTradeIndicesNoted.Contains(i))
                {
                    if(logging) Console.WriteLine($"GARBAGE PATH DETECTED! Trade {i + 1} is NOT Vital!");
                    return true;
                }
            }

            return false;
        }

        private void PrintActiveInventory(Dictionary<string, double> inv)
        {
            List<string> invStringList = new List<string>();
            foreach(KeyValuePair<string, double> item in inv)
            {
                invStringList.Add($"{item.Value}x {item.Key}");
            }
            Console.WriteLine($"Active Inv: {String.Join(", ", invStringList.ToArray())}");
        }

        private bool TradeContainsProbability(Trade trade)
        {
            foreach(Item resultItem in trade.ResultItems)
            {
                if(resultItem.Quantity < 1)
                {
                    return true;
                }
            }

            return false;
        }

        private List<Dictionary<string, int>> GenerateListOfCorrespondingIndicesForVitalTrades(List<Trade> path, string targetName, int targetAmount, bool logging)
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
                                if (logging) Console.WriteLine($"Marking index {j} as a Vital Trade for trade at index {i} ({path[i].StringSummary()})");
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

        private void UpdateTradeCount(Trade trade, bool logging, ref Dictionary<Trade, int> counts)
        {
            if (counts.ContainsKey(trade))
            {
                if (logging) Console.WriteLine($"Changing count for Trade: [{trade.StringSummary()}] from {counts[trade]} to {counts[trade] + 1}");
                counts[trade] = counts[trade] + 1;
            }
            else
            {
                if (logging) Console.WriteLine($"Adding new trade execution for: [{trade.StringSummary()}]");
                counts[trade] = 1;
            }
        }

        private double QuantityOfCostItemNeededForTrade(Trade trade, string costItemName)
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


        private bool SimulateTradeExecution(Trade trade, ref Dictionary<string, double> activeInventory)
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

            // round each value in active inventory to hundreth's place
            foreach(KeyValuePair<string, double> kvp in activeInventory)
            {
                activeInventory[kvp.Key] = Math.Round(kvp.Value, 2);
            }

            // trade has been verified possible and has been simulated, active inventory is up to date
            return true;
        }



        // from the inventory Dict, generate a list of "source" items whose amounts present will be the basis of the path's upfront cost
        private HashSet<string> SourceItemsFromInventoryDict(List<Item> inventory)
        {
            HashSet<string> sourceItems = new HashSet<string>();
            foreach(Item item in inventory)
            {
                if (item.Quantity > 0)
                {
                    sourceItems.Add(item.Name);
                    //Console.WriteLine($"Source item: {item.Name} ({item.Quantity})");
                }
            }
            return sourceItems;
        }

        public void PrintOptionPackageList(List<OptionPackage> optionPackageList, int maxOptions)
        {
            Console.WriteLine($"\nValid Options: {optionPackageList.Count}");
            for(int i = 0; i < optionPackageList.Count && (i < maxOptions || maxOptions == -1); i++)
            {
                Console.WriteLine($"\n--- OPTION #{i + 1} of {optionPackageList.Count}:");
                if(optionPackageList[i].ContainsProbability) Console.WriteLine("WARNING: Cost is approximate, as this option includes recycling item(s) with probability-based yield(s)\n");
                PrintPath(optionPackageList[i].Path);
                Console.WriteLine($"\nTotal upfront cost for this option:");
                foreach (KeyValuePair<string, double> costPair in optionPackageList[i].UpfrontCost)
                {
                    Console.WriteLine($" - {costPair.Value}x {costPair.Key}");
                }
                optionPackageList[i].PrintOptionSummary();
            }
        }

        private void PrintPath(List<Trade> path)
        {
            List<string> strList = new List<string>();
            foreach (Trade trade in path)
            {
                strList.Add(trade.StringSummary());
            }
            Console.WriteLine($"Trade Path: {string.Join(" -> ", strList)}");
        }

        
    }
}
