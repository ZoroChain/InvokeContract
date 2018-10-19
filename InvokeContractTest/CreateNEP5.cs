using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class CreateNEP5 : IExample
    {
        public string Name => "create 发布一个合约";

        public string ID => "0";

        private string chainHash;
        private string wif;
        private string contractPath;
        public string ChainHash { get => chainHash; set => chainHash = value; }
        public string WIF { get => wif; set => wif = value; }
        public string ContractPath { get => contractPath; set => contractPath = value; }

        public async Task StartAsync()
        {
            Console.WriteLine("Params:ChainHash,WIF,ContractPath");
            var param = Console.ReadLine();
            string[] messages = param.Split(",");
            Console.WriteLine("ChainHash:{0}, WIF:{1}, ContractPath:{2}", messages[0], messages[1], messages[2]);
            ChainHash = messages[0];
            WIF = messages[1];
            ContractPath = messages[2];

            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            byte[] script = System.IO.File.ReadAllBytes(ContractPath);
            Console.WriteLine("合约脚本：" + ThinNeo.Helper.Bytes2HexString(script));
            Console.WriteLine("合约脚本Hash：" + ThinNeo.Helper.Bytes2HexString(ThinNeo.Helper.GetScriptHashFromScript(script).data.ToArray().Reverse().ToArray()));
            byte[] parameter__list = ThinNeo.Helper.HexString2Bytes("0710");
            byte[] return_type = ThinNeo.Helper.HexString2Bytes("05");
            int need_storage = 1;
            int need_nep4 = 0;
            int need_canCharge = 4;
            string name = "mygas";
            string version = "1.0";
            string auther = "LZ";
            string email = "0";
            string description = "0";
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                var ss = need_storage | need_nep4 | need_canCharge;
                sb.EmitPushString(description);
                sb.EmitPushString(email);
                sb.EmitPushString(auther);
                sb.EmitPushString(version);
                sb.EmitPushString(name);
                sb.EmitPushNumber(ss);
                sb.EmitPushBytes(return_type);
                sb.EmitPushBytes(parameter__list);
                sb.EmitPushBytes(script);
                sb.EmitSysCall("Zoro.Contract.Create");

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                postArray.AddArrayValue(ChainHash);
                postArray.AddArrayValue(scriptPublish);

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
                var result = await Helper.HttpPost(url, postdata);
                //return;

                MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;

                var consume = json_result_obj["gas_consumed"].ToString();

                decimal gas_consumed = decimal.Parse(consume);

                ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
                extdata.script = sb.ToArray();

                //extdata.gas = Math.Ceiling(gas_consumed);
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

                url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                result = await Helper.HttpPost(url, postdata);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }
    }
}
