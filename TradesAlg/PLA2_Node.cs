using Newtonsoft.Json.Linq;
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
        public Trade VitalTrade;
        public List<PLA2_Node> VitalTradeCostNodes;
        public PLA2_Node ChildNode;
        private HashSet<string> ExistingNodeItemNames;
        
        public PLA2_Node(string itemName, PLA2_Node childNode, List<Trade> path, List<string> sourceItems, HashSet<string> existingNodeItemNames)
        {
            ExistingNodeItemNames = existingNodeItemNames;
            ExistingNodeItemNames.Add(itemName);

            ItemName = itemName;

            Console.WriteLine($"Created new Node for '{ItemName}'...");

            ChildNode = childNode;

            if (ChildNode != null) { Console.WriteLine($"... with Child Node = '{ChildNode.ItemName}'"); }

            VitalTrade = DetermineVitalTrade(path, sourceItems);

            if(VitalTrade != null) Console.WriteLine($"Vital trade for '{ItemName}': {VitalTrade.StringSummary()}");

            VitalTradeCostNodes = DetermineAndCreateVitalTradeCostNodes(path, sourceItems);
        }

        private Trade DetermineVitalTrade(List<Trade> path, List<string> sourceItems)
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
                    Trade trade = path[i];
                    foreach (Item item in trade.ResultItems)
                    {
                        if(item.Name == ItemName)
                        {
                            return trade;
                        }
                    }
                }
            }
            Console.WriteLine($"%%% ERROR: DetermineVitalTrade() -> {ItemName} processed incorrectly %%%");
            return null;
        }

        private List<PLA2_Node> DetermineAndCreateVitalTradeCostNodes(List<Trade> path, List<string> sourceItems)
        {
            if(VitalTrade == null) { return null; }
            
            List<PLA2_Node> vitalTradeCostNodes = new List<PLA2_Node>();

            foreach (Item costItem in VitalTrade.CostItems)
            {
                string costItemName = costItem.Name;
                if (!ExistingNodeItemNames.Contains(costItemName))
                {
                    Console.WriteLine($"Adding '{costItemName}' as a Vital Trade Cost Item for '{ItemName}'");

                    // remove last Trade in path for next set of Node creation(s)
                    List<Trade> shortenedPath = new List<Trade>(path);
                    shortenedPath.RemoveAt(shortenedPath.Count - 1);

                    PLA2_Node costNode = new PLA2_Node(costItemName, this, shortenedPath, sourceItems, ExistingNodeItemNames);
                    vitalTradeCostNodes.Add(costNode);
                }
                else
                {
                    Console.WriteLine($"Node already exists for {costItemName}, skipping");
                }
            }

            return vitalTradeCostNodes;
        }        
    }
}
