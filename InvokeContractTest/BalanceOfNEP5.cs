using System;
using System.Threading.Tasks;
using Zoro;
using Neo.VM;

namespace InvokeContractTest
{
    class BalanceOfNEP5 : IExample
    {
        public string Name => "balanceOf 获取余额";

        public string ID => "3";

        public async Task StartAsync()
        {
            string WIF = Config.getValue("WIF");
            string ContractHash = Config.getValue("ContractHash");
            string[] ChainHashList = Config.getStringArray("ChainHashList");
            string nativeNEP5 = Config.getValue("NativeNEP5");
            UInt256.TryParse(nativeNEP5, out UInt256 nativeNEP5AssetId);
            UInt160 address = ZoroHelper.GetPublicKeyHashFromWIF(WIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                if (nativeNEP5AssetId != null)
                {
                    sb.EmitSysCall("Zoro.NativeNEP5.BalanceOf", nativeNEP5AssetId, address);
                }
                else
                {
                    sb.EmitAppCall(ZoroHelper.Parse(ContractHash), "balanceOf", address);
                }

                if (Program.ChainID == "Zoro")
                {
                    foreach (var chainHash in ChainHashList)
                    {
                        var result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                        PrintBalance(result, chainHash);
                    }
                }
                else
                {
                    string chainHash = UInt160.Zero.ToString();
                    var result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    PrintBalance(result, chainHash);
                }
            }
        }

        void PrintBalance(string result, string chainHash)
        {
            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;

            if (json_result_array.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result_obj["stack"] as MyJson.JsonNode_Array;

                if (stack != null && stack.Count >= 1)
                {
                    string value = ZoroHelper.GetJsonValue(stack[0] as MyJson.JsonNode_Object);
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
