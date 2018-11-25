using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Globalization;
using ThinNeo;
using Zoro;
using Zoro.Cryptography;
using Zoro.Ledger;
using Zoro.Network.P2P.Payloads;
using Zoro.SmartContract;
using Zoro.Wallets;
using System.Threading;

namespace InvokeContractTest
{
    class Program
    {
        public static string local = Config.getValue("RpcUrl");

        public static string id_GAS = "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";

        public static string ChainID = Config.getValue("ChainID");

        static void Main(string[] args)
        {
            Config.init("Config.json");

            InitExample();
            ShowMenu();

            AsyncLoop();

            while (true) {
                System.Threading.Thread.Sleep(100);
            }
        }

        public static Dictionary<string, IExample> allExample = new System.Collections.Generic.Dictionary<string, IExample>();
        static void RegExample(IExample example)
        {
            allExample[example.ID.ToLower()] = example;
        }
        static void InitExample()
        {
            RegExample(new CreateNEP5());
            RegExample(new DeployNEP5());
            RegExample(new InvokeNEP5());
            RegExample(new BalanceOfNEP5());
            RegExample(new TransferNEP5());
            RegExample(new CocurrentNEP5Transfer());
            RegExample(new CocurrentNEP5Transfer2());
            RegExample(new Deployment());
            RegExample(new CreateAppChain());
            RegExample(new ChangeSeedList());
            RegExample(new ChangeValidators());
            RegExample(new LocalTest_HashSet());
        }

        static void ShowMenu()
        {
            Console.WriteLine("===all test===");
            foreach (var item in allExample)
            {
                Console.WriteLine("type '" + item.Key + "' to Run: " + item.Value.Name);               
            }
            Console.WriteLine("type '?' to Get this list.");
        }

        async static void AsyncLoop()
        {
            while (true)
            {
                var line = Console.ReadLine().ToLower();
                if (line == "?" || line == "？" || line == "ls")
                {
                    ShowMenu();
                }
                else if (line == "")
                {
                    continue;
                }
                else if (allExample.ContainsKey(line))
                {                    
                    var example = allExample[line];                    
                    try
                    {
                        Console.WriteLine("[begin]" + example.Name);                        
                        await example.StartAsync();

                        Console.WriteLine("[end]" + example.Name);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    Console.WriteLine("unknown line.");

                }
            }
        }
    }   
}
