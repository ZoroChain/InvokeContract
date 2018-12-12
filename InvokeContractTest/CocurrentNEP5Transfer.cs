using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading;
using Zoro;
using Zoro.Ledger;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class CocurrentNEP5Transfer : IExample
    {
        public string Name => "CocurrentNEP5Transfer 开启并发交易";

        public string ID => "5";

        public string ContractHash { get; private set; }
        public string[] ChainHashList { get; private set; }

        private KeyPair keypair;
        private UInt160 scriptHash;
        private UInt160 targetscripthash;
        private UInt256 nativeNEP5AssetId;
        private Fixed8 gasPrice = Fixed8.FromDecimal((decimal)0.001);
        public string transferValue;
        public int transNum = 0;
        private int interval = 0;
        private int stop = 0;

        protected async Task testTransfer(int idx, int chainIdx)
        {
            string chainHash = ChainHashList[chainIdx];

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                if (nativeNEP5AssetId != null)
                {
                    sb.EmitSysCall("Zoro.NativeNEP5.Transfer", nativeNEP5AssetId, scriptHash, targetscripthash, BigInteger.Parse(transferValue));
                }
                else
                {
                    sb.EmitAppCall(ZoroHelper.Parse(ContractHash), "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));
                }              

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, chainHash, gasPrice, Config.GasPrice);
                //MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                //Console.WriteLine(resJO.ToString());

                int tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine(tid + " " + idx + ": " + "sendrawtransaction " + transferValue + " chain " + chainIdx);

                if (interval > 0)
                {
                    Thread.Sleep(interval);
                }
            }
        }

        public async void RunTransferTask()
        {
            Random rd = new Random();

            int chainNum = ChainHashList.Length;

            if (transNum > 0)
            {
                for (var i = 0; i < transNum; i++)
                {
                    await testTransfer(i, rd.Next(0, chainNum));
                }
            }
            else
            {
                int i = 0;
                while (stop == 0)
                {
                    await testTransfer(i ++, rd.Next(0, chainNum));
                }
            }
        }

        public async Task StartAsync()
        {
            Console.WriteLine("输入并发的数量:");
            var param1 = Console.ReadLine();
            Console.WriteLine("发送几次交易");
            var param2 = Console.ReadLine();
            Console.WriteLine("转账金额");
            var param3 = Console.ReadLine();
            Console.WriteLine("间隔时间");
            var param4 = Console.ReadLine();
            Console.WriteLine("start {0} Thread {1} Transaction {2} Interval {3}", param1, param2, param3, param4);

            this.transNum = int.Parse(param2);
            this.interval = int.Parse(param4);
            this.transferValue = param3;

            ChainHashList = Config.getStringArray("ChainHashList");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            ContractHash = Config.getValue("ContractHash");
            string nativeNEP5 = Config.getValue("NativeNEP5");
            UInt256.TryParse(nativeNEP5, out nativeNEP5AssetId);

            Console.WriteLine($"From:{WIF.ToString()}");
            Console.WriteLine($"To:{targetWIF.ToString()}");
            Console.WriteLine($"Value:{transferValue}");

            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            stop = 0;

            for (int i = 0; i < int.Parse(param1); i++)
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
