using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

class Program
{
    static void Main(string[] args)
    {
        // Path to Program.cs class
        string programDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");

        // Load Trades data
        JArray tradesArray = LoadJArray(Path.Combine(programDir, "tradesDebug.json"));

        // Load Inventory data
        JArray inventoryArray = LoadJArray(Path.Combine(programDir, "inventoryDebug.json"));

        // Print Inventory
        Console.WriteLine("Inventory:");
        foreach (JObject item in inventoryArray)
        {
            Console.WriteLine($"{item["Name"]}: {item["Quantity"]}");
        }

        // convert Inventory into a dictionary for easier checks
        Dictionary<string, int> inventoryDict = InventoryJArrayToDictionary(inventoryArray);

        // set the target Item to find trades for
        string targetName = "J";

        // find all possible trades!
        List<List<JObject>> pathList = FindTrades(inventoryDict, tradesArray, targetName);

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










    }

    private static List<List<JObject>> RemoveDuplicateSteps(List<List<JObject>> pathList)
    {
        List<List<JObject>> distinctPathList = new List<List<JObject>>();
        
        foreach(List<JObject> path in pathList)
        {
            List<JObject> distinctPath = new List<JObject>();
            
            foreach(JObject trade in path)
            {
                bool isDistinctTrade = true;
                
                foreach (JObject distinctTrade in distinctPath)
                {
                    if(TradeToStringSummary(trade) == TradeToStringSummary(distinctTrade))
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

    private static List<List<JObject>> FindTrades(Dictionary<string, int> inventoryDict, JArray trades, string targetItemName, List<string> analyzedTargetItems = null)
    {       
        // start tracking already analyzed target items if needed
        if(analyzedTargetItems == null)
        {
            analyzedTargetItems = new List<string>();
        }
        
        JArray targetTrades = new JArray { };
        foreach (JObject trade in trades)   // find all trades that result in the current target item
        {
            foreach(var resultItem in trade["result"])
            {
                if(resultItem.Value<string>("name") == targetItemName)
                {
                    targetTrades.Add(trade);
                    break;
                }
            }
        }
      
        List<List<JObject>> pathsFound = new List<List<JObject>>();

        if(targetTrades.Count > 0)  // if at least one trade is possible for the target item in question
        {
            foreach (JObject trade in targetTrades)
            {
                if (IsTradePossible(trade, inventoryDict))  // if trade is immediately possible, return true
                {
                    //Console.WriteLine($"--- Direct trade possible for {targetItemName} -> {TradeToStringSummary(trade)}");                  
                    List<JObject> path = new List<JObject>();
                    path.Add(trade);
                    pathsFound.Add(path);
                }
                else   // otherwise determine if trade is possible via other connected trades
                {
                    //Console.WriteLine($"--- Direct trade NOT possible for {targetItemName}... looking for indirect trades");
                    List<List<List<JObject>>> pathListList = new List<List<List<JObject>>>();
                    
                    // for each cost Item for this trade, recursively call FindTrades on that item, and add the resulting list to the <<<List>>> pathListList
                    foreach (var costItem in trade["cost"])
                    {
                        string costItemName = costItem.Value<string>("name");
                        //Console.WriteLine($"--- Checking trades for {targetItemName} cost item {costItem.Value<string>("name")}");

                        // create a new list including the current target item and already analyzed target items to pass to the recurssive FindTrades call
                        List<string> updatedAnalyzedTargetItems = new List<string> { targetItemName }; 
                        updatedAnalyzedTargetItems.AddRange(analyzedTargetItems);

                        //Console.WriteLine("Already checked trades for: " + string.Join(", ", updatedAnalyzedTargetItems));

                        // only consider trades for that cost item if it has not yet been analyzed as a target item
                        if (!analyzedTargetItems.Contains(costItemName))
                        {
                            List<List<JObject>> costItemTrades = FindTrades(inventoryDict, trades, costItemName, updatedAnalyzedTargetItems);
                            if (costItemTrades != null)
                            {
                                pathListList.Add(costItemTrades);
                            }
                        }                       
                    }

                    // find all potential combinations of paths leading to the above costItems, and add any/all found to the pathsFound list
                    List<List<JObject>> indirectPaths = CombineAndConsolidateAllPossibleAltPathLists(pathListList);

                    // add the current trade to the start of each path in the indirect path list
                    foreach (List<JObject> path in indirectPaths)
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

    private static List<List<JObject>> CombineAndConsolidateAllPossibleAltPathLists(List<List<List<JObject>>> pathListList)
    {
        // base case for empty list:
        if (pathListList.Count == 0)
        {
            return new List<List<JObject>> { };
        }

        // base case for 1 element: if only one path in pathListList, return that path list alone in the <<List>>
        List<List<JObject>> firstPathList = (pathListList.First()).ToList();
        if(pathListList.Count <= 1)  
        {
            return firstPathList;
        }

        // recursively call this method on the remainder of pathLists in the pathListList
        List<List<JObject>> remainderPathList = CombineAndConsolidateAllPossibleAltPathLists(pathListList.Skip(1).ToList());

        List<List<JObject>> consolidatedPathList = new List<List<JObject>>();

        // combine firstPathList and remainderPathList into one list based on all possible combinations between the lists

        foreach (List<JObject> pathA in firstPathList)
        {
            foreach(List<JObject> pathB in remainderPathList)
            {
                List<JObject> combinedList = pathA.Concat(pathB).ToList();
                consolidatedPathList.Add(combinedList);
            }
        }

        return consolidatedPathList;
    }

    static bool IsTradePossible(JObject trade, Dictionary<string, int> inventoryDict)
    {
        foreach(var costItem in trade["cost"])
        {
            string costItemName = costItem.Value<string>("name");
            int costItemQuantity = costItem.Value<int>("quantity");

            if (!inventoryDict.ContainsKey(costItemName) || inventoryDict[costItemName] < costItemQuantity)
            {
                return false; // inventory doesn't contain enough of the required item
            }
        }

        return true; // all required items & their quantities are available
    }

    static Dictionary<string, int> InventoryJArrayToDictionary(JArray inventory)
    {
        Dictionary<string, int> inventoryDict = new Dictionary<string, int>();

        foreach (JObject item in inventory)
        {
            string itemName = item.Value<string>("Name");
            int itemQuantity = item.Value<int>("Quantity");

            if (!inventoryDict.ContainsKey(itemName))
            {
                inventoryDict[itemName] = itemQuantity;
            }
            else
            {
                inventoryDict[itemName] += itemQuantity;
            }
        }

        return inventoryDict;
    }




    static JArray LoadJArray(string jsonFilePath)
    {
        // Read the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content into JArray
        return JArray.Parse(jsonContent);
    }

    private static void PrintPathList(List<List<JObject>> pathList)
    {       
        foreach (List<JObject> path in pathList)
        {
            List<string> strList = new List<string>();
            foreach (JObject trade in path)
            {
                strList.Add(TradeToStringSummary(trade));
            }
            Console.WriteLine($"Trade Path: {string.Join(" -> ", strList)}");
        }      
    }


    static string TradeToStringSummary(JObject trade)
    {
        List<string> costList = new List<string>();
        List<string> resultList = new List<string>();

        foreach (JObject item in trade["cost"])
        {
            costList.Add((string)item["name"]);
        }
        foreach (JObject item in trade["result"])
        {
            resultList.Add((string)item["name"]);
        }

        return $"{(string)trade["category"]} {string.Join(", ", costList)} for {string.Join(", ", resultList)}";

    }
}
