using System;
using System.Threading.Tasks;
using Zoro;
using Neo.VM;

namespace InvokeContractTest
{
    class InvokeNativeNEP5 : IExample
    {
        public string Name => "InvokeNativeNEP5 查询NativeNEP5的信息";

        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string nativeNEP5Hash = Config.getValue("NativeNEP5");

            await InvokeNativeNep5Async(chainHash, nativeNEP5Hash);
        }

        public async Task InvokeNativeNep5Async(string chainHash, string nativeNEP5Hash)
        {
            UInt160 nativeNEP5AssetId = ZoroHelper.Parse(nativeNEP5Hash);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Name", nativeNEP5AssetId);
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Symbol", nativeNEP5AssetId);
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "TotalSupply", nativeNEP5AssetId);
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Decimals", nativeNEP5AssetId);

                var result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                //Console.WriteLine(result);

                MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result_obj["stack"].AsList();

                if (stack.Count == 4)
                {
                    Console.WriteLine("name:" + Helper.GetJsonString(stack[0] as MyJson.JsonNode_Object));
                    Console.WriteLine("symbol:" + Helper.GetJsonString(stack[1] as MyJson.JsonNode_Object));
                    Console.WriteLine("totalSupply:" + Helper.GetJsonBigInteger(stack[2] as MyJson.JsonNode_Object));                    
                    Console.WriteLine("decimals:" + Helper.GetJsonInteger(stack[3] as MyJson.JsonNode_Object));
                }
            }

        }
    }
}
