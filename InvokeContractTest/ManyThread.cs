using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ThinNeo;

namespace InvokeContractTest
{
    class ManyThread:IExample
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

        public byte[] prikey;
        public byte[] pubkey;
        public string address;
        public Hash160 scripthash;
        public string targetAddress;
        public Hash160 targetscripthash;
        public string transferValue;
        public int transNum = 0;

        protected void addTransWitness(ThinNeo.Transaction tran, byte[] signdata, byte[] pubkey, string addrs)
        {
            var vscript = ThinNeo.Helper.GetScriptFromPublicKey(pubkey);

            //iscript 对个人账户见证人他是一条pushbytes 指令

            var sb = new ThinNeo.ScriptBuilder();
            sb.EmitPushBytes(signdata);

            var iscript = sb.ToArray();

            tran.AddWitnessScript(vscript, iscript);
        }

        protected void testTransfer(ThinNeo.ScriptBuilder sb, int tid, int idx)
        {
            ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
            extdata.script = sb.ToArray();

            //extdata.gas = Math.Ceiling(gas_consumed - 10);
            extdata.gas = 0;

            ThinNeo.Transaction tran = Helper.makeTran(null, null, null, extdata.gas);
            tran.version = 1;
            tran.extdata = extdata;
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            //附加鉴证
            byte[] nonce = new byte[8];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce);
            tran.attributes = new ThinNeo.Attribute[2];
            tran.attributes[0] = new ThinNeo.Attribute();
            tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
            tran.attributes[0].data = scripthash;
            tran.attributes[1] = new ThinNeo.Attribute();
            tran.attributes[1].usage = TransactionAttributeUsage.Remark;
            tran.attributes[1].data = nonce;

            byte[] msg = tran.GetMessage();
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            addTransWitness(tran, signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
            postRawArray.AddArrayValue(ChainHash);
            postRawArray.AddArrayValue(rawdata);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            var result = Helper.HttpPost(url, postdata);
            Console.WriteLine(tid + " " + idx + ": " + address + " " + targetAddress + "  " + transferValue);
        }
        

        public void ThreadMethodAsync(ThinNeo.ScriptBuilder sb)
        {
            int ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            for (var i = 0;i < transNum;i ++)
            {
                testTransfer(sb, ThreadId, i);

                //using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
                //{
                //    byte[] randomBytes = new byte[32];
                //    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                //    {
                //        rng.GetBytes(randomBytes);
                //    }
                //    BigInteger randomNum = new BigInteger(randomBytes);
                //    sb.EmitPushNumber(randomNum);
                //    sb.Emit(ThinNeo.VM.OpCode.DROP);
                //    sb.EmitParamJson(array);
                //    sb.EmitPushString("transfer");
                //    sb.EmitAppCall(new Hash160(ContractHash));

            }
        }

        public async Task StartAsync()
        {
            //Console.WriteLine("Params:ChainHash,WIF,targetWIF,ContractHash,transferValue");
            //var param = Console.ReadLine();
            //string[] messages = param.Split(",");
            //Console.WriteLine("ChainHash:{0}, WIF:{1}, targetWIF:{2}, ContractPath:{3}, transferValue:{4}", messages[0], messages[1], messages[2], messages[3], messages[4]);
            //ChainHash = messages[0];
            //WIF = messages[1];
            //targetWIF = messages[2];
            //ContractHash = messages[3];
            //transferValue = messages[4];

            Console.WriteLine("开启几条线程:");
            var param1 = Console.ReadLine();
            Console.WriteLine("发送几次交易");
            var param2 = Console.ReadLine();
            Console.WriteLine("start {0} Thread {1} Transaction", param1, param2);

            this.transNum = int.Parse(param2);

            ChainHash = Config.getValue("ChainHash");
            WIF = Config.getValue("WIF");
            targetWIF = Config.getValue("targetWIF");
            ContractHash = Config.getValue("ContractHash");
            transferValue = Config.getValue("transferValue");

            Console.WriteLine(WIF.ToString());
            Console.WriteLine(targetWIF.ToString());

            prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            byte[] tragetprikey = ThinNeo.Helper.GetPrivateKeyFromWIF(targetWIF);
            byte[] targetpubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(tragetprikey);
            targetAddress = ThinNeo.Helper.GetAddressFromPublicKey(targetpubkey);
            targetscripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetAddress);

            ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(addr)" + address);//from
            array.AddArrayValue("(addr)" + targetAddress);//to
            array.AddArrayValue("(int)" + transferValue);//value
            sb.EmitParamJson(array);
            sb.EmitPushString("transfer");
            sb.EmitAppCall(new Hash160(ContractHash));

            for (int i = 0; i < int.Parse(param1); i++)
            {
                Thread t = new Thread(() => ThreadMethodAsync(sb));
                t.Start();
                //Task.Factory.StartNew(ThreadMethodAsync);
            }
        }
    }
}
