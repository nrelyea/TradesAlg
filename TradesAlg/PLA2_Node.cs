﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradesAlg
{
    public class PLA2_Node
    {
        public string ItemName;
        JObject VitalTrade;
        List<PLA2_Node> VitalTradeCostNodes;
        PLA2_Node ChildNode;
        
        public PLA2_Node(string itemName, PLA2_Node childNode, List<JObject> path, List<string> sourceItems)
        {
            ItemName = itemName;

            Console.WriteLine($"Created new Node for '{ItemName}'...");

            ChildNode = childNode;

            if (ChildNode != null) { Console.WriteLine($"... with Child Node = '{ChildNode.ItemName}'"); }

            VitalTrade = DetermineVitalTrade(path, sourceItems);

            Console.WriteLine($"Vital trade for '{ItemName}': {TradeToStringSummary(VitalTrade)}");

            VitalTradeCostNodes = DetermineAndCreateVitalTradeCostNodes(path, sourceItems);

  
            
        }

        private JObject DetermineVitalTrade(List<JObject> path, List<string> sourceItems)
        {
            if (sourceItems.Contains(ItemName))
            {
                Console.WriteLine($"'{ItemName}' is a source item, no vital trade");
                return null;
            }
            else
            {
                for(int i = path.Count - 1; i >= 0; i--)
                {
                    JObject trade = path[i];
                    foreach (JObject item in trade["result"])
                    {
                        if((string)item["name"] == ItemName)
                        {
                            return trade;
                        }
                    }
                }
            }
            Console.WriteLine($"%%% ERROR: DetermineVitalTrade() -> {ItemName} processed incorrectly %%%");
            return null;
        }

        private List<PLA2_Node> DetermineAndCreateVitalTradeCostNodes(List<JObject> path, List<string> sourceItems)
        {
            if(VitalTrade == null) { return null; }
            
            List<PLA2_Node> vitalTradeCostNodes = new List<PLA2_Node>();

            foreach (var costItem in VitalTrade["cost"])
            {
                string costItemName = costItem.Value<string>("name");
                Console.WriteLine($"Adding '{costItemName}' as a Vital Trade Cost Item for '{ItemName}'");
                PLA2_Node costNode = new PLA2_Node(costItemName, this, path, sourceItems);
                vitalTradeCostNodes.Add(costNode);
            }

            return vitalTradeCostNodes;
        }

        private string TradeToStringSummary(JObject trade)
        {
            if(trade == null) { return "N/A"; }
            
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
}
