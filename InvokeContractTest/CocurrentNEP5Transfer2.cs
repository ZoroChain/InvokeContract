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
    class CocurrentNEP5Transfer2 : IExample
    {
        public string Name => "CocurrentNEP5Transfer2 开启并发交易";

        public string ID => "6";

        public string ContractHash { get; private set; }

        private UInt160 scriptHash;
        private KeyPair keypair;
        private UInt160 targetscripthash;
        private UInt256 nativeNEP5AssetId;
        private Fixed8 gasPrice = Fixed8.FromDecimal((decimal)0.001);
        private string transferValue;
        private int cocurrentNum = 0;
        private int transNum = 0;
        private int stop = 0;

        protected async void nep5Transfer(int idx, string chainHash)
        {
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
            Console.WriteLine("start {0} Thread {1} Transaction {2}", param1, param2, param3);

            this.transNum = int.Parse(param2);
            this.cocurrentNum = int.Parse(param1);
            this.transferValue = param3;

            string[] chainHashList = Config.getStringArray("ChainHashList");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            ContractHash = Config.getValue("ContractHash");
            string nativeNEP5 = Config.getValue("NativeNEP5");
            UInt256.TryParse(nativeNEP5, out nativeNEP5AssetId);

            Console.WriteLine(WIF.ToString());
            Console.WriteLine(targetWIF.ToString());

            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);

            targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            stop = 0;

            Task.Run(() => RunTask(chainHashList));

            Console.WriteLine("输入任意键停止:");
            var input = Console.ReadLine();
            Interlocked.Exchange(ref stop, 1);
        }

        public void RunTask(string[] chainHashList)
        {
            Random rd = new Random();

            int chainNum = chainHashList.Length;

            TimeSpan oneSecond = TimeSpan.FromSeconds(1);

            int idx = 0;
            int total = 0;

            int cc = Math.Min(cocurrentNum, 10);
            int step = Math.Max(cocurrentNum / 10, 1);

            int lastWaiting = 0;
            int waitingNum = 0;
            int pendingNum = 0;

            while (stop == 0)
            {
                if (transNum > 0)
                {
                    if (total >= transNum && pendingNum == 0 && waitingNum == 0)
                        break;

                    cc = Math.Min(transNum - total, cc);
                }

                Console.WriteLine($"round:{++idx}, total:{total}, tx:{cc}, pending:{pendingNum}, waiting:{waitingNum}");

                lastWaiting = waitingNum;

                Interlocked.Add(ref pendingNum, cc);
                Interlocked.Add(ref total, cc);

                DateTime dt = DateTime.Now;

                for (int i = 0; i < cc; i++)
                {
                    int j = i;
                    Task.Run(() =>
                    {
                        int index = rd.Next(0, chainNum);
                        string chainHash = chainHashList[index];

                        Interlocked.Increment(ref waitingNum);
                        Interlocked.Decrement(ref pendingNum);

                        try
                        {
                            nep5Transfer(j, chainHash);
                        }
                        catch(Exception)
                        {

                        }
                        
                        Interlocked.Decrement(ref waitingNum);
                    });
                }

                TimeSpan span = DateTime.Now - dt;

                if (span < oneSecond)
                {
                    Thread.Sleep(oneSecond - span);
                }

                if (waitingNum > cocurrentNum * 2 || pendingNum > cocurrentNum)
                {
                    cc = Math.Max(cc - step, 0);
                }
                else if (waitingNum < cocurrentNum * 2 && pendingNum < cocurrentNum)
                {
                    cc = Math.Min(cc + step, cocurrentNum);
                }
            }
        }
    }
}
