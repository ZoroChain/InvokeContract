﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class InvokeNEP5 : IExample
    {
        public string Name => "invoke";

        public string ID => "4";

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
            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            sb.EmitParamJson(array);
            sb.EmitPushString("name");
            sb.EmitAppCall(new Hash160(ContractHash));

            sb.EmitParamJson(array);
            sb.EmitPushString("totalSupply");
            sb.EmitAppCall(new Hash160(ContractHash));

            sb.EmitParamJson(array);
            sb.EmitPushString("symbol");
            sb.EmitAppCall(new Hash160(ContractHash));

            sb.EmitParamJson(array);
            sb.EmitPushString("decimals");
            sb.EmitAppCall(new Hash160(ContractHash));

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
            postArray.AddArrayValue(ChainHash);
            postArray.AddArrayValue(scriptPublish);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);

            //Console.WriteLine(result);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
            MyJson.JsonNode_Array stack = json_result_obj["stack"].AsList();

            if (stack.Count == 4)
            {
                Console.WriteLine("name:" + Helper.GetJsonString(stack[0] as MyJson.JsonNode_Object));
                Console.WriteLine("totalSupply:" + Helper.GetJsonBigInteger(stack[1] as MyJson.JsonNode_Object));
                Console.WriteLine("symbol:" + Helper.GetJsonString(stack[2] as MyJson.JsonNode_Object));
                Console.WriteLine("decimals:" + Helper.GetJsonInteger(stack[3] as MyJson.JsonNode_Object));
            }
        }
    }
}
