using System;
using System.Threading.Tasks;
using Zoro;
using Zoro.SmartContract;
using Zoro.Cryptography.ECC;
using Neo.VM;

namespace InvokeContractTest
{
    class CreateNEP5 : IExample
    {
        public string Name => "create 发布一个合约";

        public string ID => "0";

        public async Task StartAsync()
        {
            string ChainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string ContractPath = Config.getValue("ContractPath");

            await CreateNep5Async(ChainHash, WIF, ContractPath);
        }

        public async Task CreateNep5Async(string ChainHash, string WIF, string ContractPath)
        {
            byte[] prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            ECPoint pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

            byte[] script = System.IO.File.ReadAllBytes(ContractPath);
            Console.WriteLine("合约脚本Hash：" + script.ToScriptHash().ToArray().ToHexString());
            byte[] parameter__list = ZoroHelper.HexString2Bytes("0710");
            byte[] return_type = ZoroHelper.HexString2Bytes("05");
            int need_storage = 1;
            int need_nep4 = 0;
            int need_canCharge = 4;
            string name = "mygas";
            string version = "1.0";
            string auther = "LZ";
            string email = "0";
            string description = "0";
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                var ss = need_storage | need_nep4 | need_canCharge;
                sb.EmitPush(description);
                sb.EmitPush(email);
                sb.EmitPush(auther);
                sb.EmitPush(version);
                sb.EmitPush(name);
                sb.EmitPush(ss);
                sb.EmitPush(return_type);
                sb.EmitPush(parameter__list);
                sb.EmitPush(script);
                sb.EmitSysCall("Neo.Contract.Create");

                string scriptPublish = sb.ToArray().ToHexString();

                byte[] postdata;
                string url;
                if (Program.ChainID == "Zoro")
                {
                    MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                    postArray.AddArrayValue(ChainHash);
                    postArray.AddArrayValue(scriptPublish);

                    url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
                }
                else
                {
                    url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scriptPublish));
                }

                var result = await Helper.HttpPost(url, postdata);

                MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;

                var consume = json_result_obj["gas_consumed"].ToString();

                decimal gas_consumed = decimal.Parse(consume);

                result = await ZoroHelper.SendRawTransaction(sb.ToArray(), scriptHash, prikey, pubkey, ChainHash);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }
    }
}
