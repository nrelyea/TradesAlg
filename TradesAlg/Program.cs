using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using TradesAlg;

class Program
{
    static void Main(string[] args)
    {
        // Path to Program.cs class
        string programDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");

        // Load Trades data
        //List<Trade> allTradesList = LoadTradesList(Path.Combine(programDir, "tradesDebug.json"));
        List<Trade> allTradesList = LoadTradesList(Path.Combine(programDir, "tradesSample.json"));

        // Load Inventory data
        //List<Item> inventory = LoadInventory(Path.Combine(programDir, "inventoryDebug.json"));
        List<Item> inventory = LoadInventory(Path.Combine(programDir, "inventorySample.json"));

        // set the target Item to find trades for
        string targetName = "Crossbow";
        int targetAmount = 1;

        

        // find all possible trades!
        List<List<Trade>> pathList = FindTrades(inventory, allTradesList, targetName);
        
        // Remove duplicate / redundant steps in paths
        pathList = RemoveDuplicateSteps(pathList);
        
        if(pathList.Count > 0)
        {
            Console.WriteLine("\nTrade is possible!\n");
            PrintPathList(pathList);
        }
        else
        {
            Console.WriteLine("\nTrade is NOT possible.");
        }

        Console.WriteLine("\n\n\n");

        // Determine up front costs of each trade path as a dictionary of cost items and their amount

        //PathListAnalysis pla = new PathListAnalysis();
        //List<Dictionary<string, int>> upfrontCostList = pla.UpfrontCosts(pathList, targetName, targetAmount);


        PathListAnalysis2 pla2 = new PathListAnalysis2();
        List<Dictionary<string,int>> upfrontCostList = pla2.AllUpfrontCosts(inventory, pathList, targetName, targetAmount);









        
    }

    private static List<List<Trade>> RemoveDuplicateSteps(List<List<Trade>> pathList)
    {
        List<List<Trade>> distinctPathList = new List<List<Trade>>();
        
        foreach(List<Trade> path in pathList)
        {
            List<Trade> distinctPath = new List<Trade>();
            
            foreach(Trade trade in path)
            {
                bool isDistinctTrade = true;
                
                foreach (Trade distinctTrade in distinctPath)
                {
                    if(trade.StringSummary() == distinctTrade.StringSummary())
                    {
                        isDistinctTrade = false;
                        break;
                    }
                }

                if (isDistinctTrade)
                {
                    distinctPath.Add(trade);
                }
            }

            distinctPathList.Add(distinctPath);
        }

        return distinctPathList;
    }

    private static List<List<Trade>> FindTrades(List<Item> inventory, List<Trade> trades, string targetItemName, List<string> analyzedTargetItems = null)
    {       
        // start tracking already analyzed target items if needed
        if(analyzedTargetItems == null)
        {
            analyzedTargetItems = new List<string>();
        }
        
        List<Trade> targetTrades = new List<Trade> { };
        foreach (Trade trade in trades)   // find all trades that result in the current target item
        {
            foreach(Item resultItem in trade.ResultItems)
            {
                if(resultItem.Name == targetItemName)
                {
                    targetTrades.Add(trade);
                    break;
                }
            }
        }
      
        List<List<Trade>> pathsFound = new List<List<Trade>>();

        if(targetTrades.Count > 0)  // if at least one trade is possible for the target item in question
        {
            foreach (Trade trade in targetTrades)
            {
                //Console.WriteLine($"Considering trade '{trade.StringSummary()}' that could result in {targetItemName}");
                if (trade.IsPossible(inventory))  // if trade is immediately possible, return true
                {
                    //Console.WriteLine($"--- Direct trade possible for {targetItemName} -> {trade.StringSummary()}");                  
                    List<Trade> path = new List<Trade>();
                    path.Add(trade);
                    pathsFound.Add(path);
                }
                else   // otherwise determine if trade is possible via other connected trades
                {
                    //Console.WriteLine($"--- Direct trade NOT possible for {targetItemName}... looking for indirect trades");
                    List<List<List<Trade>>> pathListList = new List<List<List<Trade>>>();
                    
                    // for each cost Item for this trade, recursively call FindTrades on that item, and add the resulting list to the <<<List>>> pathListList
                    foreach (Item costItem in trade.CostItems)
                    {
                        string costItemName = costItem.Name;
                        //Console.WriteLine($"--- Checking trades for {targetItemName} cost item {costItemName}");

                        // create a new list including the current target item and already analyzed target items to pass to the recurssive FindTrades call
                        List<string> updatedAnalyzedTargetItems = new List<string> { targetItemName }; 
                        updatedAnalyzedTargetItems.AddRange(analyzedTargetItems);

                        //Console.WriteLine("Already checked trades for: " + string.Join(", ", updatedAnalyzedTargetItems));

                        // skip recurssive FindTrades call for this cost item if it is already in the inventory
                        bool skipRecurssiveFindTradesCall = false;
                        foreach(Item item in inventory)
                        {
                            if (item.Name == costItemName && item.Quantity > 0)
                            {
                                //Console.WriteLine($"--- Inventory contains {costItemName}, skipping further search for trades on this item");
                                skipRecurssiveFindTradesCall = true;
                                break;
                            }
                        }
                        if(skipRecurssiveFindTradesCall) { continue; }

                        // only consider trades for that cost item if it has not yet been analyzed as a target item
                        if (!analyzedTargetItems.Contains(costItemName))
                        {
                            List<List<Trade>> costItemTrades = FindTrades(inventory, trades, costItemName, updatedAnalyzedTargetItems);
                            if (costItemTrades != null)
                            {
                                pathListList.Add(costItemTrades);
                            }
                        }                       
                    }

                    // find all potential combinations of paths leading to the above costItems, and add any/all found to the pathsFound list
                    List<List<Trade>> indirectPaths = CombineAndConsolidateAllPossibleAltPathLists(pathListList);

                    // add the current trade to the start of each path in the indirect path list
                    foreach (List<Trade> path in indirectPaths)
                    {
                        path.Add(trade);
                    }

                    // add final set of indirect paths from this trade to the pathsFound list
                    pathsFound.AddRange(indirectPaths);
                }
            }
        }
        else
        {
            Console.WriteLine($"--- No trades possible for {targetItemName}");
            return null;   // no trades exist for the target item
        }

        return pathsFound;
    }

    private static List<List<Trade>> CombineAndConsolidateAllPossibleAltPathLists(List<List<List<Trade>>> pathListList)
    {
        // base case for empty list:
        if (pathListList.Count == 0)
        {
            return new List<List<Trade>> { };
        }

        // base case for 1 element: if only one path in pathListList, return that path list alone in the <<List>>
        List<List<Trade>> firstPathList = (pathListList.First()).ToList();
        if(pathListList.Count <= 1)  
        {
            return firstPathList;
        }

        // recursively call this method on the remainder of pathLists in the pathListList
        List<List<Trade>> remainderPathList = CombineAndConsolidateAllPossibleAltPathLists(pathListList.Skip(1).ToList());

        List<List<Trade>> consolidatedPathList = new List<List<Trade>>();

        // combine firstPathList and remainderPathList into one list based on all possible combinations between the lists

        foreach (List<Trade> pathA in firstPathList)
        {
            foreach(List<Trade> pathB in remainderPathList)
            {
                List<Trade> combinedList = pathA.Concat(pathB).ToList();
                consolidatedPathList.Add(combinedList);
            }
        }

        return consolidatedPathList;
    }

    

    static List<Item> LoadInventory(string jsonFilePath)
    {
        // Read the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content into JArray
        JArray inventoryArray = JArray.Parse(jsonContent);

        List<Item> inventory = new List<Item>();

        foreach (JObject item in inventoryArray)
        {
            string itemName = item.Value<string>("Name");
            int itemQuantity = item.Value<int>("Quantity");
            Console.WriteLine($"Inventory item: {itemQuantity} {itemName}");
            inventory.Add(new Item(itemName, itemQuantity));
        }

        return inventory;
    }




    static List<Trade> LoadTradesList(string jsonFilePath)
    {
        // Read the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content into JArray
        JArray tradesArray = JArray.Parse(jsonContent);

        List<Trade> trades = new List<Trade>();

        foreach(JObject tradeObj in tradesArray)
        {
            string category = tradeObj.Value<string>("category");
            
            List<Item> costItems = new List<Item>();
            foreach (var costItem in tradeObj["cost"])
            {
                string costItemName = costItem.Value<string>("name");
                int costItemQuantity = costItem.Value<int>("quantity");
                costItems.Add(new Item(costItemName, costItemQuantity));
            }

            List<Item> resultItems = new List<Item>();
            foreach (var resultItem in tradeObj["result"])
            {
                string resultItemName = resultItem.Value<string>("name");
                int resultItemQuantity = resultItem.Value<int>("quantity");
                resultItems.Add(new Item(resultItemName, resultItemQuantity));
            }

            trades.Add(new Trade(category, costItems, resultItems));
        }

        return trades;
    }

    private static void PrintPathList(List<List<Trade>> pathList)
    {       
        foreach (List<Trade> path in pathList)
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
