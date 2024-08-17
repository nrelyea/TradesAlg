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



        // Load Inventory data
        //List<Item> inventory = LoadInventory(Path.Combine(programDir, "inventoryDebug.json"));
        List<Item> inventory = LoadInventory(Path.Combine(programDir, "inventorySample.json"));

        // Target item & quantity criteria
        string targetName = "crossbow";
        int targetAmount = 4;

        // Trades list generation criteria
        int workBenchLevel = 3;
        bool safeZoneRecycler = true;
        bool outPostTradesEnabled = true;
        bool banditTradesEnabled = true;
        string itemDataPath = Path.Combine(programDir, "itemsbyname.json");
        string marketDataPath = Path.Combine(programDir, "marketTrades.json");

        string generatedTradesPath = Path.Combine(programDir, "tradesGenerated.json");

        TradeGeneration tg = new TradeGeneration(
            workBenchLevel, safeZoneRecycler, outPostTradesEnabled, banditTradesEnabled, itemDataPath, marketDataPath, generatedTradesPath
        );

        List<Trade> allTradesList = tg.GeneratedTrades;

        Console.WriteLine($"Trades generated: {allTradesList.Count}");


        // Load Trades data
        //List<Trade> allTradesList = LoadTradesList(Path.Combine(programDir, "tradesDebug.json"));
        //List<Trade> allTradesList = LoadTradesList(Path.Combine(programDir, "tradesSample.json"));





        PathGeneration pg = new PathGeneration();

        // find all possible trades!
        int depth = 4;
        List<List<Trade>> pathList = pg.FindTrades(inventory, allTradesList, targetName, depth);
        
        pathList = pg.RemoveGarbagePaths(pathList);
        
        if(pathList != null && pathList.Count > 0)
        {
            // Remove duplicate / redundant steps in paths
            pathList = pg.RemoveDuplicateSteps(pathList);
            
            Console.WriteLine($"\nTrade is possible!\nPaths found: {pathList.Count}\n");
            //pg.PrintPathList(pathList);
        }
        else
        {
            Console.WriteLine("\nTrade is NOT possible.");
            return;
        }






        // Determine full option packages with upfront costs of each trade path as a dictionary of cost items and their amount
        Console.WriteLine("Calculating upfront costs for all valid options...");
        PathListAnalysis3 pla3 = new PathListAnalysis3();
        List<OptionPackage> optionPackageList = pla3.AllOptionPackages(inventory, pathList, targetName, targetAmount);

        int maxOptions = 100;
        pla3.PrintOptionPackageList(optionPackageList, maxOptions);








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
