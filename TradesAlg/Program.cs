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
        List<string> path = PathToAcquireItem(inventoryArray, tradesArray, targetName);

        if (path.Count > 0)
        {
            Console.WriteLine($"\nPath to acquire {targetName}: {string.Join(" -> ", path)}");
        } 
        else
        {
            Console.WriteLine($"\nNo paths exist to acquire {targetName}"  );
        }
        

    }

    public static List<string> PathToAcquireItem(JArray inventory, JArray trades, string targetItemName)
    {
        List<string> path = new List<string>();

        Dictionary<string, int> inventoryDict = InventoryJArrayToDictionary(inventory); // convert Inventory JArray to Dictionary

        bool tradeFound = FindTrades(inventoryDict, trades, targetItemName);
        if (tradeFound) Console.WriteLine("TRADE WORKS BOYYYYYYYY");

        return path;
    }

    public static bool FindTrades(Dictionary<string, int> inventoryDict, JArray trades, string targetItemName)
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

        if(targetTrades.Count > 0)  // if there is at least one immediate trade available for the target item in question
        {
            foreach (JObject trade in targetTrades)
            {
                if (IsTradePossible(trade, inventoryDict))  // if trade is immediately possible, return true
                {
                    Console.WriteLine($"Trade possible for {targetItemName}:");
                    //PrintTrade(trade);
                    Console.WriteLine(TradeToStringSummary(trade));
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
                        Console.WriteLine(TradeToStringSummary(trade));
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
