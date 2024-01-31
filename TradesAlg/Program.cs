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



        foreach(JObject trade in trades)
        {
            
        }


        return path;
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



    static JArray LoadJArray(string jsonFilePath)
    {
        // Read the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);

        // Parse the JSON content into JArray
        return JArray.Parse(jsonContent);
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
