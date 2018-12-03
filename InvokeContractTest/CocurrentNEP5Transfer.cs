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
    class CocurrentNEP5Transfer : IExample
    {
        public string Name => "ManyThread 开启并发交易";

        public string ID => "5";

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
        public string transferValue;
        public int transNum = 0;
        private int interval = 0;
        private int stop = 0;

        protected async Task testTransfer(int idx, int chainIdx)
        {
            string chainHash = ChainHashList[chainIdx];

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

                int tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine(tid + " " + idx + ": " + "sendrawtransaction " + transferValue + " chain " + chainIdx);

                try
                {
                    var result = await Helper.HttpPost(url, postdata);
                    MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                    //Console.WriteLine(resJO.ToString());
                }
                catch (Exception)
                {

                }

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

            ChainHashList = Config.getStringArray("ChainHashList");
            WIF = Config.getValue("WIF");
            targetWIF = Config.getValue("targetWIF");
            ContractHash = Config.getValue("ContractHash");
            transferValue = param3;// Config.getValue("transferValue");

            Console.WriteLine(WIF.ToString());
            Console.WriteLine(targetWIF.ToString());

            prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

            tragetprikey = ZoroHelper.GetPrivateKeyFromWIF(targetWIF);
            targetpubkey = ZoroHelper.GetPublicKeyFromPrivateKey(tragetprikey);
            targetscripthash = ZoroHelper.GetPublicKeyHash(targetpubkey);

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
