using System;
using System.Threading.Tasks;
using Zoro;
using Neo.VM;

namespace InvokeContractTest
{
    class InvokeBCP : IExample
    {
        public string Name => "InvokeBCP 查询BCP的信息";

        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string BCPHash = Config.getValue("BCPHash");

            await InvokeBCPAsync(chainHash, BCPHash);
        }

        public async Task InvokeBCPAsync(string chainHash, string BCPHash)
        {
            UInt256 assetId = UInt256.Parse(BCPHash);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.GlobalAsset.Name", assetId);
                sb.EmitSysCall("Zoro.GlobalAsset.FullName", assetId);
                sb.EmitSysCall("Zoro.GlobalAsset.Amount", assetId);
                sb.EmitSysCall("Zoro.GlobalAsset.GetPrecision", assetId);

                var result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                //Console.WriteLine(result);

                MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result_obj["stack"].AsList();

                if (stack.Count == 4)
                {
                    Console.WriteLine("Name:" + Helper.GetJsonString(stack[0] as MyJson.JsonNode_Object));
                    Console.WriteLine("FullName:" + Helper.GetJsonString(stack[1] as MyJson.JsonNode_Object));
                    Console.WriteLine("Amount:" + Helper.GetJsonBigInteger(stack[2] as MyJson.JsonNode_Object));                    
                    Console.WriteLine("Precision:" + Helper.GetJsonInteger(stack[3] as MyJson.JsonNode_Object));
                }
            }

        }
    }
}
