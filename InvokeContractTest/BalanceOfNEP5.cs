using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class BalanceOfNEP5 : IExample
    {
        public string Name => "balanceOf";

        public string ID => "3";

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

        public async Task StartAsync()
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            var scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(addr)" + address);
            sb.EmitParamJson(array);
            sb.EmitPushString("balanceOf");
            sb.EmitAppCall(new Hash160(ContractHash));

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
            postArray.AddArrayValue(ChainHash);
            postArray.AddArrayValue(scriptPublish);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
            MyJson.JsonNode_Array stack = json_result_obj["stack"].AsList();

            if (stack.Count == 1)
            {
                Console.WriteLine("balanceOf:" + Helper.GetJsonBigInteger(stack[0] as MyJson.JsonNode_Object));
            }
        }
    }
}
