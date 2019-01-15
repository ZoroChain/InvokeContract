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
            RegExample(new RetrieveBCP(), "0");
            RegExample(new Deployment(), "1");
            RegExample(new BalanceOf(), "2");
            RegExample(new TransferTest(), "3");
            RegExample(new CocurrentTransfer(), "4");
            RegExample(new CrossChainTransaction(), "5");
            RegExample(new ConcurrentCrossChain(), "6");
            RegExample(new CreateContract(), "7");
            RegExample(new ContractTest(), "8");
            RegExample(new CreateNEP5(), "11");
            RegExample(new DeployNEP5(), "12");
            RegExample(new InvokeNEP5(), "13");
            RegExample(new CreateNativeNEP5(), "14");
            RegExample(new DeployNativeNEP5(), "15");
            RegExample(new InvokeNativeNEP5(), "16");            
            RegExample(new CreateAppChain(), "17");
            RegExample(new ChangeSeedList(), "18");
            RegExample(new ChangeValidators(), "19");
            RegExample(new LocalTest_HashSet(), "20");
            RegExample(new LocalTest_LevelDB(), "21");
            RegExample(new LocalTest_ConcurrentDictionary(), "22");
            RegExample(new LocalTest_MemPool(), "23");
            RegExample(new LocalTest_Signature(), "24");
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
