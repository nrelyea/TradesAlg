using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

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

        string targetName = "E";
        PathToAcquireItem(inventoryArray, tradesArray, targetName);

        //if (path.Count > 0)
        //{
        //    Console.WriteLine($"\nPath to acquire {targetName}: {string.Join(" -> ", path)}");
        //} 
        //else
        //{
        //    Console.WriteLine($"\nNo paths exist to acquire {targetName}"  );
        //}



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














    }

    public static void PathToAcquireItem(JArray inventory, JArray trades, string targetItemName)
    {
        List<string> path = new List<string>();

        Dictionary<string, int> inventoryDict = InventoryJArrayToDictionary(inventory); // convert Inventory JArray to Dictionary

        //List<List<JObject>> pathsFound = FindTrades(inventoryDict, trades, targetItemName);

        //if (pathsFound.Count > 0) Console.WriteLine("TRADE WORKS BOYYYYYYYY");

        //return path;
    }

    /*public static List<List<JObject>> FindTrades(Dictionary<string, int> inventoryDict, JArray trades, string targetItemName, List<JObject> currentPath = null)
    {
        if (currentPath == null)
        {
            currentPath = new List<JObject>();  
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

        if(targetTrades.Count > 0)  // if there is at least one immediate trade available for the target item in question
        {
            foreach (JObject trade in targetTrades)
            {
                if (IsTradePossible(trade, inventoryDict))  // if trade is immediately possible, return true
                {
                    Console.WriteLine($"Trade possible for {targetItemName}:");
                    //PrintTrade(trade);                   
                    return true;
                }
                else   // otherwise determine if trade is possible via other connected trades
                {
                    bool tradePossibleViaAnother = true;    // Remains true if all other trades related to the base item for this one can be traded for using target item
                    foreach (var costItem in trade["cost"])
                    {
                        if(!FindTrades(inventoryDict, trades, costItem.Value<string>("name")))
                        {
                            tradePossibleViaAnother = false;
                        }
                    }

                    if(tradePossibleViaAnother)
                    {
                        Console.WriteLine($"Trade possible for {targetItemName} via its other trades:");
                        //PrintTrade(trade);
                        return true;
                    }
                }
            }
        }
        else
        {
            return false;   // no immediate trades available for the target item
        }

        return false;
    }*/

    private static List<List<string>> CombineAndConsolidateAllPossibleAltPathLists(List<List<List<string>>> pathListList)
    {
        // base case, if only one path in pathListList, return that path list alone in the 
        List<List<string>> firstPathList = (pathListList.First()).ToList();
        if(pathListList.Count <= 1)  
        {
            return firstPathList;
        }

        // recursively call this method on the remainder of pathLists in the pathListList
        List<List<string>> remainderPathList = CombineAndConsolidateAllPossibleAltPathLists(pathListList.Skip(1).ToList());

        List<List<string>> consolidatedPathList = new List<List<string>>();

        // combine firstPathList and remainderPathList into one list based on all possible combinations between the lists

        foreach (List<string> pathA in firstPathList)
        {
            foreach(List<string> pathB in remainderPathList)
            {
                List<string> combinedList = pathA.Concat(pathB).ToList();
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
