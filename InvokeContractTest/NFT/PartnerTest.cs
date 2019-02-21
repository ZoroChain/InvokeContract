using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Zoro;
using Zoro.IO;
using Zoro.IO.Json;
using Zoro.Ledger;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class PartnerTest:IExample
    {
        public string Name => "NFT 并发交易";

        private UInt160 scriptHash;
        private KeyPair keypair;
        private UInt160 targetAddress;
        private UInt160 nftHash;
        private UInt160 BCPAssetId;
        private int concurrencyCount = 0;
        private int transferCount = 0;
        private int waitingNum = 0;
        private int error = 0;
        private bool randomTargetAddress = false;
        private bool randomBuyCount = false;
        private UInt160[] targetAddressList;
        private Fixed8 gasLimit = Fixed8.FromDecimal(8000);

        private CancellationTokenSource cancelTokenSource;

        protected async void CallTransfer(string chainHash, UInt160 targetAddress, int buyCount)
        {
            Interlocked.Increment(ref waitingNum);


            await NftBuyTest(chainHash, targetAddress, buyCount);


            Interlocked.Decrement(ref waitingNum);
        }

        protected async Task NftBuyTest(string chainHash, UInt160 targetAddress, int buyCount)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(nftHash, "buy", targetAddress, (BigInteger)buyCount);
                //Console.WriteLine(buyCount);
                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(6000 * buyCount), Fixed8.FromDecimal(0.0001m));

                ParseResult(result);
            }
        }

        public async Task StartAsync()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 512;

            await Task.Run(() => Test());
        }

        private void Test()
        {
            Console.Write("输入并发的数量:");
            var param2 = Console.ReadLine();
            Console.Write("发送几次交易:");
            var param3 = Console.ReadLine();
            Console.Write("目标账户随机, 0 - no, 1 - yes:");
            var param5 = Console.ReadLine();
            Console.Write("购买数量随机, 0 - no, 1 - yes:");
            var param6 = Console.ReadLine();

            transferCount = int.Parse(param3);
            concurrencyCount = int.Parse(param2);
            randomTargetAddress = int.Parse(param5) == 1;
            randomBuyCount = int.Parse(param6) == 1;
            string[] chainHashList = Config.getStringArray("ChainHashList");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            targetAddress = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);
            nftHash = UInt160.Parse(Config.getValue("nftHash"));
            BCPAssetId = Genesis.BcpContractAddress;

            if (randomTargetAddress)
            {
                InitializeRandomTargetAddressList(transferCount);
            }

            cancelTokenSource = new CancellationTokenSource();

            Task.Run(() => RunTask(chainHashList));

            Console.WriteLine("输入回车键停止:");
            var input = Console.ReadLine();
            cancelTokenSource.Cancel();
        }

        public void RunTask(string[] chainHashList)
        {
            int chainNum = chainHashList.Length;

            Random rnd = new Random();
            TimeSpan oneSecond = TimeSpan.FromSeconds(1);

            int idx = 0;
            int total = 0;

            int cc = concurrencyCount;

            int lastWaiting = 0;
            int pendingNum = 0;

            waitingNum = 0;
            error = 0;

            while (true)
            {
                if (cancelTokenSource.IsCancellationRequested)
                {
                    Console.WriteLine("停止发送交易.");
                    break;
                }

                if (transferCount > 0)
                {
                    if (total >= transferCount && pendingNum == 0 && waitingNum == 0)
                    {
                        Console.WriteLine($"round:{++idx}, total:{total}, tx:0, pending:{pendingNum}, waiting:{waitingNum}, error:{error}");
                        break;
                    }

                    cc = Math.Min(transferCount - total, cc);
                }

                Console.WriteLine($"round:{++idx}, total:{total}, tx:{cc}, pending:{pendingNum}, waiting:{waitingNum}, error:{error}");

                lastWaiting = waitingNum;

                if (cc > 0)
                {
                    Interlocked.Add(ref pendingNum, cc);
                    Interlocked.Add(ref total, cc);
                }

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

                        int buyCount = 1;

                        if (randomBuyCount)
                            buyCount = rnd.Next(1, 50);

                        CallTransfer(chainHash, randomTargetAddress ? GetRandomTargetAddress(rnd) : targetAddress, buyCount);
                    });
                }

                TimeSpan span = DateTime.Now - dt;

                if (span < oneSecond)
                {
                    Thread.Sleep(oneSecond - span);
                }
            }
        }

        protected void InitializeRandomTargetAddressList(int count)
        {
            int maximum = 50000;
            count = Math.Min(maximum, count);

            string filename = "targetaddress.dat";
            if (!LoadTargetAddress(filename, count))
            {
                GenerateRandomTargetAddressList(filename, count);
            }
        }

        protected bool LoadTargetAddress(string filename, int count)
        {
            if (!File.Exists(filename))
                return false;

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, true))
                {
                    int num = reader.ReadInt32();
                    if (num < count)
                        return false;

                    targetAddressList = new UInt160[num];
                    for (int i = 0; i < num; i++)
                    {
                        targetAddressList[i] = reader.ReadSerializable<UInt160>();
                    }

                    return true;
                }
            }
        }

        protected void GenerateRandomTargetAddressList(string filename, int count)
        {
            Console.WriteLine($"Generating random target address list:{count}");

            DateTime time = DateTime.UtcNow;

            targetAddressList = new UInt160[count];
            for (int i = 0; i < count; i++)
            {
                targetAddressList[i] = GenerateRandomTargetAddress();
                if (i % 100 == 0)
                {
                    Console.Write(".");
                }
            }

            TimeSpan interval = DateTime.UtcNow - time;

            Console.WriteLine($"Target address list completed, time:{interval:hh\\:mm\\:ss\\.ff}");

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII, true))
                {
                    writer.Write(count);
                    for (int i = 0; i < count; i++)
                    {
                        writer.Write(targetAddressList[i]);
                    }
                }
            }
        }

        protected UInt160 GenerateRandomTargetAddress()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }

            KeyPair key = new KeyPair(privateKey);
            return key.PublicKeyHash;
        }

        protected UInt160 GetRandomTargetAddress(Random rnd)
        {
            int index = rnd.Next(0, targetAddressList.Length);
            return targetAddressList[index];
        }

        private void ParseResult(string result)
        {
            if (result.Length == 0 || !IsSendRawTxnResultOK(result))
            {
                Interlocked.Increment(ref error);
            }
        }

        private void ParseInvokeResult(string result)
        {
            if (result.Length == 0 || !IsInvokeResultOK(result))
            {
                Interlocked.Increment(ref error);
            }
        }

        public bool IsSendRawTxnResultOK(string response)
        {
            try
            {
                JObject json_response = JObject.Parse(response);
                JObject json_result = json_response["result"];
                return json_result.AsBoolean();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        public bool IsInvokeResultOK(string response)
        {
            try
            {
                JObject json_response = JObject.Parse(response);
                JObject json_result = json_response["result"];
                JObject json_state = json_result["state"];
                string state = json_state.AsString();
                return state.Length > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

    }
}
