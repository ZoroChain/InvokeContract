using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class DeployNEP5 : IExample
    {
        public string Name => "deploy 调用nep5合约中的deploy方法";

        public string ID => "1";

        private string chainHash;
        private string wif;
        private string contractHash;
        public string ChainHash { get => chainHash; set => chainHash = value; }
        public string WIF { get => wif; set => wif = value; }
        public string ContractHash { get => contractHash; set => contractHash = value; }

        public async Task StartAsync()
        {
            //Console.WriteLine("Params:ChainHash,WIF,ContractHash");
            //var param = Console.ReadLine();
            //string[] messages = param.Split(",");
            //Console.WriteLine("ChainHash:{0}, WIF:{1}, ContractPath:{2}", messages[0], messages[1], messages[2]);
            //ChainHash = messages[0];
            //WIF = messages[1];
            //ContractHash = messages[2];

            ChainHash = Config.getValue("ChainHash");
            WIF = Config.getValue("WIF");
            ContractHash = Config.getValue("ContractHash");

            await DeployNEP5Async(ChainHash, WIF, ContractHash);
        }

        public async Task DeployNEP5Async(string ChainHash, string WIF, string ContractHash) {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(int)1");
                sb.EmitParamJson(array);
                sb.EmitPushString("deploy");
                sb.EmitAppCall(new Hash160(ContractHash));

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                byte[] postdata;

                ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                extdata.script = sb.ToArray();

                //extdata.gas = Math.Ceiling(gas_consumed - 10);
                extdata.gas = 0;

                ThinNeo.Transaction tran = Helper.makeTran(null, null, new ThinNeo.Hash256(Program.id_GAS), extdata.gas);
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
                postRawArray.AddArrayValue(ChainHash);
                postRawArray.AddArrayValue(rawdata);

                var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                var result = await Helper.HttpPost(url, postdata);
            }
        }
    }
}
