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
        string api = "https://api.nel.group/api/testnet";
        public static string local = "http://127.0.0.1:23332/";

        public static string id_GAS = "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";  


        static void Main(string[] args)
        {
            InitExample();
            ShowMenu();

            AsyncLoop();

            while (true) {
                System.Threading.Thread.Sleep(100);
            }
        }

        static Dictionary<string, IExample> allExample = new System.Collections.Generic.Dictionary<string, IExample>();
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
            RegExample(new ManyThread());
        }

        static void ShowMenu()
        {
            Console.WriteLine("===all test===");
            foreach (var item in allExample)
            {
                Console.WriteLine("type '" + item.Key + "' to Run: " + item.Value.Name);               
            }
            Console.WriteLine("params write for example: ChainHash,WIF,targetWIF,ContractPath,ContractHash");
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
                    Console.WriteLine("Params:ChainHash,WIF,targetWIF,ContractPath,ContractHash");
                    var param = Console.ReadLine();
                    var example = allExample[line];
                    string[] messages = param.Split(",");
                    try
                    {
                        Console.WriteLine("[begin]" + example.Name);
                        Console.WriteLine("ChainHash:{0}, WIF:{1}, targetWIF:{2}, ContractPath:{3}, ContractHash:{4}", messages[0], messages[1], messages[2], messages[3], messages[4]);
                        example.ChainHash = messages[0];
                        example.WIF = messages[1];
                        example.targetWIF = messages[2];
                        example.ContractPath = messages[3];
                        example.ContractHash = messages[4];
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
