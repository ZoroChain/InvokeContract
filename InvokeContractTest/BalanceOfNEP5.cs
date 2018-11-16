﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class BalanceOfNEP5 : IExample
    {
        public string Name => "balanceOf 获取余额";

        public string ID => "3";

        public string WIF { get; private set; }
        public string ContractHash { get; private set; }
        public string[] ChainHashList { get; private set; }

        public async Task StartAsync()
        {
            ChainHashList = Config.getStringArray("ChainHashList");
            WIF = Config.getValue("WIF");
            ContractHash = Config.getValue("ContractHash");

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

            Console.WriteLine($"Address: {WIF.ToString()}");

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());
            string url;
            byte[] postdata;
            if (ChainHashList.Length > 0)
            {
                foreach (var chainHash in ChainHashList)
                {
                    MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                    postArray.AddArrayValue(chainHash);
                    postArray.AddArrayValue(scriptPublish);

                    url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
                    await BalanceOf(chainHash, url, postdata);
                }
            }
            else
            {
                url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scriptPublish));
                await BalanceOf("", url, postdata);
            }
        }

        async Task BalanceOf(string chainHash, string url, byte[] postdata)
        {
            var result = await Helper.HttpPost(url, postdata);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;

            if (json_result_array.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result_obj["stack"] as MyJson.JsonNode_Array;

                if (stack != null && stack.Count >= 1)
                {
                    string value = Helper.GetJsonBigInteger(stack[0] as MyJson.JsonNode_Object);
                    Console.WriteLine($"balanceOf {chainHash}: {value}");
                }
            }
            else if(json_result_array.ContainsKey("error"))
            {
                MyJson.JsonNode_Object json_error_obj = json_result_array["error"] as MyJson.JsonNode_Object;
                Console.WriteLine($"balanceOf {chainHash}: {json_error_obj}");
            }
        }
    }
}
