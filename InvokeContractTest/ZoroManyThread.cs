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
    class ZoroManyThread : IExample
    {
        public string Name => "ManyThread 开启多线程多次交易";

        public string ID => "5";

        private string chainHash;
        private string wif;
        private string targetwif;
        private string contractHash;
        public string ChainHash { get => chainHash; set => chainHash = value; }
        public string WIF { get => wif; set => wif = value; }
        public string targetWIF { get => targetwif; set => targetwif = value; }
        public string ContractHash { get => contractHash; set => contractHash = value; }

        private byte[] prikey;
        private Zoro.Cryptography.ECC.ECPoint pubkey;
        private UInt160 scriptHash;
        private byte[] tragetprikey;
        private Zoro.Cryptography.ECC.ECPoint targetpubkey;
        private UInt160 targetscripthash;
        public string transferValue;
        public int transNum = 0;

        protected void testTransfer(int tid, int idx)
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
                    ChainHash = ZoroHelper.Parse(ChainHash),
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
                if (ChainHash.Length > 0)
                {
                    MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                    postRawArray.AddArrayValue(ChainHash);
                    postRawArray.AddArrayValue(rawdata);

                    url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                }
                else
                {
                    url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
                }

                var result = Helper.HttpPost(url, postdata);
                Console.WriteLine(tid + " " + idx + ": " + "sendrawtransaction " + transferValue);
            }
        }


        public void ThreadMethodAsync()
        {
            int ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            for (var i = 0; i < transNum; i++)
            {
                testTransfer(ThreadId, i);
            }
        }

        public async Task StartAsync()
        {
            Console.WriteLine("开启几条线程:");
            var param1 = Console.ReadLine();
            Console.WriteLine("发送几次交易");
            var param2 = Console.ReadLine();
            Console.WriteLine("转账金额");
            var param3 = Console.ReadLine();
            Console.WriteLine("start {0} Thread {1} Transaction {2}", param1, param2, param3);

            this.transNum = int.Parse(param2);

            ChainHash = Config.getValue("ChainHash");
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

            for (int i = 0; i < int.Parse(param1); i++)
            {
                await Task.Factory.StartNew(ThreadMethodAsync);
            }
        }
    }
}
