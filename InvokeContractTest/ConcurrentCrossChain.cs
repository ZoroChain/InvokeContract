using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThinNeo;
using Zoro;
using Zoro.IO;
using Zoro.Network.P2P;
using Zoro.Network.P2P.Payloads;
using Transaction = ThinNeo.Transaction;
using TransactionOutput = ThinNeo.TransactionOutput;

namespace InvokeContractTest
{
    public class ConcurrentCrossChain : IExample
    {
        public string Name => "ManyThread 开启并发交易";

        public string ID => "12";
        public string WIF { get; private set; }
        public string ContractHash { get; private set; }
        private byte[] prikey;
        
        private byte[] tragetprikey;
        private int direction = 1;
        private string chainHash;
        public string transferValue;
        public int transNum = 0;
        private int interval = 0;
        private int stop = 0;
        private string scriptHash;
        private string targetscripthash;
        private string api;
        private static string id_gas;
        private static List<string> usedUtxoList = new List<string>(); //本区块内同一账户已使用的 UTXO 记录
        private static int neoTransHeight = 0;
        public string ChainHash
        {
            get => chainHash;
            set => chainHash = value;
        }

        public async Task StartAsync()
        {
            Console.WriteLine("输入兑换方向，1：NEO --> Zoro; 2: Zoro --> Neo ");
            var param0 = Console.ReadLine();
            direction = int.Parse(param0);
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
            ChainHash = Config.getValue("ChainHash");
            WIF = Config.getValue("CrossAccount");
            if (direction == 1)
            {
                ContractHash = Config.getValue("NeoBcp");
                targetscripthash = Config.getValue("NeoBank");
                api = Config.getValue("NeoRpcUrl");
                id_gas = Config.getValue("id_GAS");
            }
            if (direction == 2)
            {
                ContractHash = Config.getValue("ZoroBcp");
                targetscripthash = Config.getValue("ZoroBank");
            }

            stop = 0;
            transferValue = param3;
            for (int i = 0; i < int.Parse(param1); i++)
            {
                RunTestTask(param0);
            }
            if (transNum == 0)
            {
                Console.WriteLine("输入任意键停止:");
                var input = Console.ReadLine();
                Interlocked.Exchange(ref stop, 1);
            }
        }

        public void RunTestTask(string param0)
        {
            Task.Run(async () => { await RunTransferTask(); });
        }

        public async Task RunTransferTask()
        {
            Random rd = new Random();

            if (transNum > 0)
            {
                for (var i = 0; i < transNum; i++)
                {
                    if (direction == 1)
                        await testNeoTransfer();
                    if (direction == 2)
                        await testZoroTransfer();
                }
            }
            else
            {
                int i = 0;
                while (stop == 0)
                {
                    if (direction == 1)
                        await testNeoTransfer();
                    if (direction == 2)
                        await testZoroTransfer();
                }
            }
        }

        protected async Task testZoroTransfer()
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            var address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(addr)" + address);
            array.AddArrayValue("(hex160)" + targetscripthash); //ZoroBank address   AUB7tMoKTzN33iVVqhz98vnT3KiG4bqx3f
            array.AddArrayValue("(int)" + transferValue);
            byte[] randomBytes = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            BigInteger randomNum = new BigInteger(randomBytes);
            sb.EmitPushNumber(randomNum);
            sb.Emit(ThinNeo.VM.OpCode.DROP);
            sb.EmitParamJson(array);
            sb.EmitPushString("transfer");
            sb.EmitAppCall(new Hash160(ContractHash));
            Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            byte[] postdata;
            ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
            extdata.script = sb.ToArray();
            extdata.gas = 0;
            ThinNeo.Transaction tran = new Transaction();
            tran.inputs = new ThinNeo.TransactionInput[0];
            tran.outputs = new TransactionOutput[0];
            tran.attributes = new ThinNeo.Attribute[1];
            tran.attributes[0] = new ThinNeo.Attribute();
            tran.version = 1;
            tran.extdata = extdata;
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            //附加鉴证
            tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
            tran.attributes[0].data = scripthash;

            byte[] msg = tran.GetMessage();
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
            postRawArray.AddArrayValue(chainHash); //根链
            postRawArray.AddArrayValue(rawdata);

            var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);
            Console.WriteLine(result + " txid: " + txid);
        }

        protected async Task testNeoTransfer()
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            var address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            byte[] script = null;
            using (var sb = new ThinNeo.ScriptBuilder())
            {
                var array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(addr)" + address);
                array.AddArrayValue("(hex160)" + targetscripthash);//NeoBank address   AMjCDmrbfcBxGPitHcdrUYRyPXD7DfC52c
                array.AddArrayValue("(int)" + transferValue);
                byte[] randomBytes = new byte[32];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                BigInteger randomNum = new BigInteger(randomBytes);
                sb.EmitPushNumber(randomNum);
                sb.Emit(ThinNeo.VM.OpCode.DROP);
                sb.EmitParamJson(array);//参数倒序入
                sb.EmitPushString("transfer");//参数倒序入
                sb.EmitAppCall(new Hash160(ContractHash));//nep5脚本
                script = sb.ToArray();
            }

            if (IsClear())
                usedUtxoList.Clear();
            Dictionary<string, List<Utxo>> dic_UTXO =
                await Helper.GetBalanceByAddress(Config.getValue("NelRpcUrl"), address);
            if (dic_UTXO.ContainsKey(Config.getValue("id_GAS")) == false)
            {
                Console.WriteLine("No gas");
            }
            Transaction tran = Helper.makeGasTran(dic_UTXO[id_gas], ref usedUtxoList, null, new ThinNeo.Hash256(id_gas), decimal.Parse(Config.getValue("gasFee")));

            tran.inputs = new ThinNeo.TransactionInput[0];
            tran.outputs = new TransactionOutput[0];
            tran.attributes = new ThinNeo.Attribute[1];
            tran.attributes[0] = new ThinNeo.Attribute();
            tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
            tran.attributes[0].data = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
            tran.version = 1;
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            var idata = new ThinNeo.InvokeTransData();

            tran.extdata = idata;
            idata.script = script;
            idata.gas = 0;

            byte[] msg = tran.GetMessage();
            string msgstr = ThinNeo.Helper.Bytes2HexString(msg);
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(api, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            var result = await Helper.HttpPost(url, postdata);
            Console.WriteLine(result + " txid: " + txid);
        }

        private bool IsClear()
        {
            var v = GetNeoHeight();
            if (v > neoTransHeight + 1)
            {
                neoTransHeight = v;
                return true;
            }
            return false;
        }

        public int GetNeoHeight()
        {
            var url = api + "?method=getblockcount&id=1&params=[]";
            var result = Helper.HttpGet(url).Result;
            var res = Newtonsoft.Json.Linq.JObject.Parse(result)["result"];
            int height = (int)res;
            return height;
        }
    }
}
