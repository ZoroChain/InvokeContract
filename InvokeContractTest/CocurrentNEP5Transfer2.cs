using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class CocurrentNEP5Transfer2 : IExample
    {
        public string Name => "CocurrentNEP5Transfer2 开启并发交易";

        public string ID => "6";

        private UInt160 scriptHash;
        private KeyPair keypair;
        private UInt160 targetAddress;
        private UInt160 nep5ContractHash;
        private UInt256 nativeNEP5AssetId;
        private string transferValue;
        private int transType = 0;
        private int cocurrentNum = 0;
        private int transNum = 0;
        private int waitingNum = 0;
        private int step = 0;

        private CancellationTokenSource cancelTokenSource;

        protected async void CallTransfer(string chainHash)
        {
            Interlocked.Increment(ref waitingNum);

            if (transType == 0)
            {
                await NativeNEP5Transfer(chainHash);
            }
            else if (transType == 1)
            {
                await NEP5Transfer(chainHash);
            }
            else if(transType == 2)
            {
                await ContranctTransfer(chainHash);
            }

            Interlocked.Decrement(ref waitingNum);
        }

        protected async Task NativeNEP5Transfer(string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.NativeNEP5.Transfer", nativeNEP5AssetId, scriptHash, targetAddress, BigInteger.Parse(transferValue));

                await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Config.GasLimit["NativeNEP5Transfer"], Config.GasPrice);
            }
        }

        protected async Task NEP5Transfer(string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitAppCall(nep5ContractHash, "transfer", scriptHash, targetAddress, BigInteger.Parse(transferValue));

                await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Config.GasLimit["NEP5Transfer"], Config.GasPrice);
            }
        }

        protected async Task ContranctTransfer(string chainHash)
        {
            BigInteger bigValue = BigInteger.Parse(transferValue);
            await ZoroHelper.SendContractTransaction(nativeNEP5AssetId, keypair, targetAddress, new Fixed8((long)bigValue), chainHash, Config.GasPrice);
        }

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        {
            Console.Write("选择交易类型，0 - NativeNEP5, 1 - NEP5 SmartContract, 2 - ContractTransaction：");
            var param1 = Console.ReadLine();
            Console.Write("输入并发的数量：");
            var param2 = Console.ReadLine();
            Console.Write("发送几次交易：");
            var param3 = Console.ReadLine();
            Console.Write("转账金额：");
            var param4 = Console.ReadLine();
            Console.Write("是否自动调整并发数量：");
            var param5 = Console.ReadLine();

            transType = int.Parse(param1);
            transNum = int.Parse(param3);
            cocurrentNum = int.Parse(param2);
            transferValue = param4;
            step = int.Parse(param5) == 1 ? 10 : 0;

            string[] chainHashList = Config.getStringArray("ChainHashList");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");

            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            targetAddress = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

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
            }

            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCPortThreads);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCPortThreads);

            ThreadPool.SetMinThreads(cocurrentNum, minCPortThreads);

            cancelTokenSource = new CancellationTokenSource();

            Task.Run(() => RunTask(chainHashList));

            Console.WriteLine("输入回车键停止:");
            var input = Console.ReadLine();
            cancelTokenSource.Cancel();

            ThreadPool.SetMinThreads(minWorkerThreads, minCPortThreads);
        }

        public void RunTask(string[] chainHashList)
        {
            int chainNum = chainHashList.Length;

            TimeSpan oneSecond = TimeSpan.FromSeconds(1);

            int idx = 0;
            int total = 0;

            int cc = step > 0 ? Math.Min(cocurrentNum, step) : cocurrentNum;

            int lastWaiting = 0;
            int pendingNum = 0;

            waitingNum = 0;

            while (true)
            {
                if (cancelTokenSource.IsCancellationRequested)
                {
                    Console.WriteLine("停止发送交易.");
                    break;
                }

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
                        Random rd = new Random();
                        int index = rd.Next(0, chainNum);
                        string chainHash = chainHashList[index];

                        Interlocked.Decrement(ref pendingNum);

                        try
                        {
                            CallTransfer(chainHash);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    });
                }

                TimeSpan span = DateTime.Now - dt;

                if (span < oneSecond)
                {
                    Thread.Sleep(oneSecond - span);
                }

                if (step > 0)
                {
                    if (pendingNum > cocurrentNum)
                    {
                        cc = Math.Max(cc - step, 0);
                    }
                    else if (pendingNum < cocurrentNum)
                    {
                        cc = Math.Min(cc + step, cocurrentNum);
                    }
                }
            }
        }
    }
}
