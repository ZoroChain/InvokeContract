using System;
using System.Collections.Generic;

namespace InvokeContractTest
{
    class Program
    {
        public static string local = Config.getValue("RpcUrl");

        public static string id_GAS = "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";

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
        static void RegExample(IExample example, string id)
        {
            allExample[id.ToLower()] = example;
        }
        static void InitExample()
        {
            RegExample(new Deployment(), "0");
            RegExample(new CreateNEP5(), "1");
            RegExample(new DeployNEP5(), "2");
            RegExample(new InvokeNEP5(), "3");
            RegExample(new BalanceOf(), "4");
            RegExample(new TransferTest(), "5");
            RegExample(new CocurrentTransfer(), "6");
            RegExample(new RetrieveBCP(), "9");
            RegExample(new CreateAppChain(), "10");
            RegExample(new ChangeSeedList(), "11");
            RegExample(new ChangeValidators(), "12");            
            RegExample(new CrossChainTransaction(), "20");
            RegExample(new ConcurrentCrossChain(), "21");            
            RegExample(new CreateContract(), "22");
            RegExample(new ContractTest(), "23");
            RegExample(new LocalTest_HashSet(), "30");
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
