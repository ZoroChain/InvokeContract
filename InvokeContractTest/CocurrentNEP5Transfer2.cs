using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Zoro;
using Zoro.IO;
using Zoro.Ledger;
using Zoro.Network.P2P.Payloads;
using Neo.VM;

namespace InvokeContractTest
{
    class CocurrentNEP5Transfer2 : IExample
    {
        public string Name => "CocurrentNEP5Transfer2 开启并发交易";

        public string ID => "6";

        public string WIF { get; private set; }
        public string targetWIF { get; private set; }
        public string ContractHash { get; private set; }
        public string[] ChainHashList { get; private set; }

        private byte[] prikey;
        private Zoro.Cryptography.ECC.ECPoint pubkey;
        private UInt160 scriptHash;
        private byte[] tragetprikey;
        private Zoro.Cryptography.ECC.ECPoint targetpubkey;
        private UInt160 targetscripthash;
        private string transferValue;
        private int cocurrentNum = 0;
        private int transNum = 0;
        private int stop = 0;

        protected async void nep5Transfer(int idx, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                byte[] randomBytes = new byte[32];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                BigInteger randomNum = new BigInteger(randomBytes);
                sb.EmitPush(randomNum);
                sb.EmitPush(Neo.VM.OpCode.DROP);
                sb.EmitAppCall(ZoroHelper.Parse(ContractHash), "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                InvocationTransaction tx = new InvocationTransaction
                {
                    ChainHash = ZoroHelper.Parse(chainHash),
                    Version = 1,
                    Script = sb.ToArray(),
                    Gas = Fixed8.Zero,
                };

                tx.Inputs = new CoinReference[0];
                tx.Outputs = new TransactionOutput[0];

                tx.Attributes = new TransactionAttribute[1];
                tx.Attributes[0] = new TransactionAttribute();
                tx.Attributes[0].Usage = TransactionAttributeUsage.Script;
                tx.Attributes[0].Data = scriptHash.ToArray();

                byte[] data = ZoroHelper.GetHashData(tx);
                byte[] signdata = ZoroHelper.Sign(data, prikey, pubkey);
                ZoroHelper.AddWitness(tx, signdata, pubkey);
                string rawdata = ThinNeo.Helper.Bytes2HexString(tx.ToArray());

                string url;
                byte[] postdata;
                if (Program.ChainID == "Zoro")
                {
                    MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                    postRawArray.AddArrayValue(chainHash);
                    postRawArray.AddArrayValue(rawdata);

                    url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                }
                else
                {
                    url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
                }

                //int tid = Thread.CurrentThread.ManagedThreadId;
                //Console.WriteLine($"sendrawtransaction {idx}, tid:{tid}");

                try
                {
                    var result = await Helper.HttpPost(url, postdata);
                }
                catch (Exception)
                {

                }
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

            ChainHashList = Config.getStringArray("ChainHashList");
            WIF = Config.getValue("WIF");
            targetWIF = Config.getValue("targetWIF");
            ContractHash = Config.getValue("ContractHash");
            transferValue = param3;

            Console.WriteLine(WIF.ToString());
            Console.WriteLine(targetWIF.ToString());

            prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

            tragetprikey = ZoroHelper.GetPrivateKeyFromWIF(targetWIF);
            targetpubkey = ZoroHelper.GetPublicKeyFromPrivateKey(tragetprikey);
            targetscripthash = ZoroHelper.GetPublicKeyHash(targetpubkey);

            stop = 0;

            //ThreadPool.GetMinThreads(out int workerThreads, out int cpThreads);
            //ThreadPool.SetMinThreads(cocurrentNum, cpThreads);

            Task.Run(() => RunTask());

            Console.WriteLine("输入任意键停止:");
            var input = Console.ReadLine();
            Interlocked.Exchange(ref stop, 1);

            //ThreadPool.SetMinThreads(workerThreads, cpThreads);
        }

        public void RunTask()
        {
            Random rd = new Random();

            int chainNum = ChainHashList.Length;

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
                        string chainHash = ChainHashList[index];

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
