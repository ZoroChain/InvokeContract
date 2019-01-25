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
    class CocurrentTransfer : IExample
    {
        public string Name => "CocurrentTransfer 开启并发交易";

        private UInt160 scriptHash;
        private KeyPair keypair;
        private UInt160 targetAddress;
        private UInt160 nep5ContractHash;
        private UInt160 nativeNEP5AssetId;
        private UInt160 BCPAssetId;
        private string transferValue;
        private int transactionType = 0;
        private int concurrencyCount = 0;
        private int transferCount = 0;
        private int waitingNum = 0;
        private int error = 0;
        private bool randomTargetAddress = false;
        private bool randomGasPrice = false;
        private UInt160[] targetAddressList;

        private CancellationTokenSource cancelTokenSource;

        protected async void CallTransfer(string chainHash, UInt160 targetAddress, Fixed8 gasPrice)
        {
            Interlocked.Increment(ref waitingNum);

            if (transactionType == 0)
            {
                await NEP5Transfer(chainHash, targetAddress, gasPrice);
            }
            else if (transactionType == 1)
            {
                await NativeNEP5Transfer(chainHash, targetAddress, gasPrice);
            }
            else if (transactionType == 2)
            {
                await BCPTransfer(chainHash, targetAddress, gasPrice);
            }
            else if (transactionType == 3)
            {
                await InvokeNEP5Test(chainHash);
            }

            Interlocked.Decrement(ref waitingNum);
        }

        protected async Task NativeNEP5Transfer(string chainHash, UInt160 targetAddress, Fixed8 gasPrice)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Transfer", nativeNEP5AssetId, scriptHash, targetAddress, BigInteger.Parse(transferValue));

                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Config.GasLimit["NativeNEP5Transfer"], gasPrice);

                ParseResult(result);
            }
        }

        protected async Task NEP5Transfer(string chainHash, UInt160 targetAddress, Fixed8 gasPrice)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(nep5ContractHash, "transfer", scriptHash, targetAddress, BigInteger.Parse(transferValue));

                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Config.GasLimit["NEP5Transfer"], gasPrice);

                ParseResult(result);
            }
        }

        protected async Task BCPTransfer(string chainHash, UInt160 targetAddress, Fixed8 gasPrice)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Transfer", BCPAssetId, scriptHash, targetAddress, BigInteger.Parse(transferValue));

                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Config.GasLimit["BCPTransfer"], gasPrice);

                ParseResult(result);
            }
        }

        protected async Task InvokeNEP5Test(string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(nep5ContractHash, "name");
                sb.EmitAppCall(nep5ContractHash, "totalSupply");
                sb.EmitAppCall(nep5ContractHash, "symbol");
                sb.EmitAppCall(nep5ContractHash, "decimals");

                string result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);

                ParseInvokeResult(result);
            }
        }

        public async Task StartAsync()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 512;

            await Task.Run(() => Test());
        }

        private void Test()
        {
            Console.Write("选择交易类型，0 - NEP5 SmartContract, 1 - NativeNEP5, 2 - BCP, 3 - InvokeNEP5:");
            var param1 = Console.ReadLine();
            Console.Write("输入并发的数量:");
            var param2 = Console.ReadLine();
            Console.Write("发送几次交易:");
            var param3 = Console.ReadLine();
            Console.Write("转账金额:");
            var param4 = Console.ReadLine();
            Console.Write("目标账户随机, 0 - no, 1 - yes:");
            var param5 = Console.ReadLine();
            Console.Write("GasPrice随机, 0 - no, 1 - yes:");
            var param6 = Console.ReadLine();

            transactionType = int.Parse(param1);
            transferCount = int.Parse(param3);
            concurrencyCount = int.Parse(param2);
            transferValue = param4;
            randomTargetAddress = int.Parse(param5) == 1;
            randomGasPrice = int.Parse(param6) == 1;

            string[] chainHashList = Config.getStringArray("ChainHashList");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");

            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            targetAddress = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            string contractHash = Config.getValue("ContractHash");
            nep5ContractHash = UInt160.Parse(contractHash);

            string nativeNEP5Hash = Config.getValue("NativeNEP5");
            nativeNEP5AssetId = UInt160.Parse(nativeNEP5Hash);

            BCPAssetId = Genesis.BcpContractAddress;

            if (randomTargetAddress)
            {
                InitializeRandomTargetAddressList(transferCount);
            }

            if (transactionType == 0 || transactionType == 1 || transactionType == 2)
            {
                Console.WriteLine($"From:{WIF}");
                Console.WriteLine($"To:{targetWIF}");
                Console.WriteLine($"Count:{transferCount}");
                Console.WriteLine($"Value:{transferValue}");
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

                        Fixed8 price = Config.GasPrice;

                        if (randomGasPrice)
                            Fixed8.TryParse((rnd.Next(1, 1000) * 0.00001).ToString(), out price);

                        CallTransfer(chainHash, randomTargetAddress ? GetRandomTargetAddress(rnd) : targetAddress, price);
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
