using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class CocurrentNEP5Transfer : IExample
    {
        public string Name => "CocurrentNEP5Transfer 开启并发交易";

        public string ID => "5";

        private string[] chainHashList { get; set; }

        private KeyPair keypair;
        private UInt160 scriptHash;
        private UInt160 targetscripthash;
        private UInt160 nep5ContractHash;
        private UInt256 nativeNEP5AssetId;
        private Fixed8 GasPriceNEP5 = Fixed8.FromDecimal((decimal)4.5);
        private Fixed8 GasPriceNativeNEP5 = Fixed8.FromDecimal((decimal)0.001);
        private string transferValue;
        private int transType = 0;
        private int transNum = 0;
        private int interval = 0;
        private int stop = 0;

        protected async Task CallTransfer(int idx, int chainIdx)
        {
            string chainHash = chainHashList[chainIdx];

            if (transType == 0)
            {
                await NativeNEP5Transfer(nativeNEP5AssetId, idx, chainHash);
            }
            else if (transType == 1)
            {
                await NEP5Transfer(nep5ContractHash, idx, chainHash);
            }
        }

        protected async Task NativeNEP5Transfer(UInt256 assetId, int idx, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.NativeNEP5.Transfer", assetId, scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, chainHash, GasPriceNativeNEP5, Config.GasPrice);

                //MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                //Console.WriteLine(resJO.ToString());

                int tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"NativeNEP5Transfer ThreadId:{tid}, Index:{idx}");

                if (interval > 0)
                {
                    Thread.Sleep(interval);
                }
            }
        }

        protected async Task NEP5Transfer(UInt160 contractHash, int idx, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitAppCall(contractHash, "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, chainHash, GasPriceNEP5, Config.GasPrice);

                //MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                //Console.WriteLine(resJO.ToString());

                int tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"NEP5Transfer ThreadId:{tid}, Index:{idx}");

                if (interval > 0)
                {
                    Thread.Sleep(interval);
                }
            }
        }

        public async void RunTransferTask()
        {
            Random rd = new Random();

            int chainNum = chainHashList.Length;

            if (transNum > 0)
            {
                for (var i = 0; i < transNum; i++)
                {
                    await CallTransfer(i, rd.Next(0, chainNum));
                }
            }
            else
            {
                int i = 0;
                while (stop == 0)
                {
                    await CallTransfer(i ++, rd.Next(0, chainNum));
                }
            }
        }

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        { 
            Console.Write("选择交易类型，0 - NativeNEP5, 1 - NEP5 SmartContract：");
            var param1 = Console.ReadLine();
            Console.Write("输入并发的数量：");
            var param2 = Console.ReadLine();
            Console.Write("发送几次交易：");
            var param3 = Console.ReadLine();
            Console.Write("转账金额：");
            transferValue = Console.ReadLine();
            Console.Write("间隔时间：");
            var param4 = Console.ReadLine();

            int count = int.Parse(param2);
            transType = int.Parse(param1);
            transNum = int.Parse(param3);
            interval = int.Parse(param4);

            chainHashList = Config.getStringArray("ChainHashList");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");

            string contractHash = Config.getValue("ContractHash");
            nep5ContractHash = UInt160.Parse(contractHash);

            string nativeNEP5 = Config.getValue("NativeNEP5");
            nativeNEP5AssetId = UInt256.Parse(nativeNEP5);

            if (transType == 0 || transType == 1)
            {
                Console.WriteLine($"From:{WIF}");
                Console.WriteLine($"To:{targetWIF}");
                Console.WriteLine($"Count:{transNum}");
                Console.WriteLine($"Value:{transferValue}");
                Console.WriteLine($"Interval:{interval}");
            }

            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            stop = 0;

            for (int i = 0; i < count; i++)
            {
                RunTestTask();
            }

            if (transNum == 0)
            {
                Console.WriteLine("输入任意键停止:");
                var input = Console.ReadLine();
                Interlocked.Exchange(ref stop, 1);
            }
        }

        public void RunTestTask()
        {
            Task.Run(() => { RunTransferTask(); });
        }
    }
}
