using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;

class Program
{
    static void Main(string[] args)
    {
        // Load Trades data
        JArray tradesArray = LoadJArray("trades.json");

        // Print trades
        //foreach (JObject trade in tradesArray)
        //{
        //    PrintTrade(trade);
        //}

        // Load Inventory data
        JArray inventoryArray = LoadJArray("inventory.json");

        // Print Inventory
        Console.WriteLine("Inventory:");
        foreach (JObject item in inventoryArray)
        {
            Console.WriteLine($"{item["Name"]}: {item["Quantity"]}");
        }

        // convert Inventory into a dictionary for easier checks
        Dictionary<string, int> inventoryDict = InventoryJArrayToDictionary(inventoryArray);

        // set the target Item to find trades for
        string targetName = "F";

        // find all possible trades!
        List<List<JObject>> pathList = FindTrades(inventoryDict, tradesArray, targetName);

        if(pathList.Count > 0)
        {
            Console.WriteLine("\nTrade is possible!\n");
            PrintPathList(pathList);
        }
        else
        {
            Console.WriteLine("\nTrade is NOT possible.");
        }





        /*
        List<List<List<string>>> testPathPathList = new List<List<List<string>>>();

        List<List<string>> firstPathList = new List<List<string>>();
        firstPathList.Add(new List<string> { "A1", "A2", "A3" });
        firstPathList.Add(new List<string> { "B1", "B2", "B3" });
        testPathPathList.Add(firstPathList);

        List<List<string>> secondPathList = new List<List<string>>();
        secondPathList.Add(new List<string> { "C1", "C2", "C3" });
        secondPathList.Add(new List<string> { "D1", "D2", "D3" });
        testPathPathList.Add(secondPathList);

        List<List<string>> thirdPathList = new List<List<string>>();
        thirdPathList.Add(new List<string> { "E1", "E2", "E3" });
        thirdPathList.Add(new List<string> { "F1", "F2", "F3" });
        testPathPathList.Add(thirdPathList);



        PrintStringListLists(CombineAndConsolidateAllPossibleAltPathLists(testPathPathList));
        */













    }

    private static List<List<JObject>> PathsToAcquireItem(JArray inventory, JArray trades, string targetItemName)
    {
        Dictionary<string, int> inventoryDict = InventoryJArrayToDictionary(inventory); // convert Inventory JArray to Dictionary

        return FindTrades(inventoryDict, trades, targetItemName);
    }

    private static List<List<JObject>> FindTrades(Dictionary<string, int> inventoryDict, JArray trades, string targetItemName)
    {       
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
                    Console.WriteLine($"--- Direct trade possible for {targetItemName} -> {TradeToStringSummary(trade)}");
                    //PrintTrade(trade);                   
                    List<JObject> path = new List<JObject>();
                    path.Add(trade);
                    pathsFound.Add(path);
                }
                else   // otherwise determine if trade is possible via other connected trades
                {
                    Console.WriteLine($"--- Direct trade NOT possible for {targetItemName}... looking for indirect trades");
                    List<List<List<JObject>>> pathListList = new List<List<List<JObject>>>();
                    
                    // for each cost Item for this trade, recursively call FindTrades on that item, and add the resulting list to the <<<List>>> pathListList
                    foreach (var costItem in trade["cost"])
                    {
                        Console.WriteLine($"--- Checking trades for {targetItemName} cost item {costItem.Value<string>("name")}");
                        List<List<JObject>> costItemTrades = FindTrades(inventoryDict, trades, costItem.Value<string>("name"));
                        if (costItemTrades != null)
                        {
                            pathListList.Add(costItemTrades);
                        }
                    }

                    // find all potential combinations of paths leading to the above costItems, and add any/all found to the pathsFound list
                    List<List<JObject>> indirectPaths = CombineAndConsolidateAllPossibleAltPathLists(pathListList);

                    // add the current trade to the start of each path in the indirect path list
                    foreach (List<JObject> path in indirectPaths)
                    {
                        //path.Insert(0, trade);
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

    private static bool ListsContainSameStrings(List<string> list1, List<string> list2)
    {
        if(list1.Count != list2.Count) { return false; }
        
        foreach (string str in list1)
        {
            if(!list2.Contains(str)) return false;
        }

        return true;
    }

    private static void PrintStringListLists(List<List<string>> lstlst)
    {
        Console.WriteLine($"Collected {lstlst.Count} paths:");
        foreach (List<string> list in lstlst)
        {
            Console.WriteLine(string.Join(", ", list));
        }
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


    static void PrintTrade(JObject trade)
    {
        Console.WriteLine("\nTrade:");
        Console.Write(" Cost:  ");
        foreach (JObject item in trade["cost"])
        {
            Console.Write($"   {item["name"]}: {item["quantity"]}");
        }
        Console.Write("\n Result:");
        foreach (JObject item in trade["result"])
        {
            Console.Write($"   {item["name"]}: {item["quantity"]}");
        }
        Console.WriteLine("");
    }
}
