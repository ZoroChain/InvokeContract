using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class ManyThread:IExample
    {
        public static int ThreadID = 0;

        public string Name => "ManyThread";

        public string ID => "5";

        private string chainHash;
        private string wif;
        private string targetwif;
        private string contractPath;
        private string contractHash;
        public string ChainHash { get => chainHash; set => chainHash = value; }
        public string WIF { get => wif; set => wif = value; }
        public string targetWIF { get => targetwif; set => targetwif = value; }
        public string ContractPath { get => contractPath; set => contractPath = value; }
        public string ContractHash { get => contractHash; set => contractHash = value; }

        public byte[] prikey;
        public byte[] pubkey;
        public string address;
        public Hash160 scripthash;
        public string targetAddress;
        public Hash160 targetscripthash;

        public void ThreadMethodAsync()
        {            
            while (ThreadID < 500)
            {
                lock (typeof(ManyThread))
                {
                    ThreadID += 1;
                    using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
                    {
                        MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                        array.AddArrayValue("(addr)" + address);//from
                        array.AddArrayValue("(addr)" + targetAddress);//to
                        array.AddArrayValue("(int)" + ThreadID);//value
                        sb.EmitParamJson(array);
                        sb.EmitPushString("transfer");
                        sb.EmitAppCall(new Hash160(ContractHash));

                        string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                        MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                        postArray.AddArrayValue(ChainHash);
                        postArray.AddArrayValue(scriptPublish);

                        ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                        extdata.script = sb.ToArray();

                        //extdata.gas = Math.Ceiling(gas_consumed - 10);
                        extdata.gas = 0;

                        ThinNeo.Transaction tran = Helper.makeTran(null, null, null, extdata.gas);
                        tran.version = 1;
                        tran.extdata = extdata;
                        tran.type = ThinNeo.TransactionType.InvocationTransaction;

                        //附加鉴证
                        tran.attributes = new ThinNeo.Attribute[1];
                        tran.attributes[0] = new ThinNeo.Attribute();
                        tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
                        tran.attributes[0].data = scripthash;

                        byte[] msg = tran.GetMessage();
                        byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
                        tran.AddWitness(signdata, pubkey, address);
                        string txid = tran.GetHash().ToString();
                        byte[] data = tran.GetRawData();
                        string rawdata = ThinNeo.Helper.Bytes2HexString(data);

                        MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                        postRawArray.AddArrayValue(ContractHash);
                        postRawArray.AddArrayValue(rawdata);

                        byte[] postdata;
                        var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                        var result = Helper.HttpPost(url, postdata);
                        Console.WriteLine(address + " " + targetAddress + "  " + ThreadID);
                    }
                }
            }
        }

        public async Task StartAsync()
        {
            prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            byte[] tragetprikey = ThinNeo.Helper.GetPrivateKeyFromWIF(targetWIF);
            byte[] targetpubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(tragetprikey);
            targetAddress = ThinNeo.Helper.GetAddressFromPublicKey(targetpubkey);
            targetscripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetAddress);
            for (int i = 0; i < 20; i++)
            {
                Task.Factory.StartNew(ThreadMethodAsync);
            }
        }
    }
}
