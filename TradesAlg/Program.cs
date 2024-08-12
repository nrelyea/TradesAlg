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
        int targetAmount = 5;



        PathGeneration pg = new PathGeneration();

        // find all possible trades!
        List<List<Trade>> pathList = pg.FindTrades(inventory, allTradesList, targetName);
        
        // Remove duplicate / redundant steps in paths
        pathList = pg.RemoveDuplicateSteps(pathList);
        
        if(pathList.Count > 0)
        {
            Console.WriteLine("\nTrade is possible!\n");
            pg.PrintPathList(pathList);
        }
        else
        {
            Console.WriteLine("\nTrade is NOT possible.");
        }

        Console.WriteLine("\n\n\n");



        // Determine full option packages with upfront costs of each trade path as a dictionary of cost items and their amount

        PathListAnalysis2 pla2 = new PathListAnalysis2();
        List<OptionPackage> optionPackageList = pla2.AllOptionPackages(inventory, pathList, targetName, targetAmount);

        pla2.PrintOptionPackageList(optionPackageList);








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

    


    
}
