using System;
using System.Threading.Tasks;
using Zoro;
using Neo.VM;

namespace InvokeContractTest
{
    class InvokeNEP5 : IExample
    {
        public string Name => "InvokeNEP5 查询NEP5合约的信息";

        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string wif = Config.getValue("WIF");
            string contractHash = Config.getValue("ContractHash");

            await InvokeNep5Async(chainHash, wif, contractHash);
        }

        public async Task InvokeNep5Async(string chainHash, string WIF, string ContractHash)
        {
            UInt160 scriptHash = ZoroHelper.Parse(ContractHash);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, "name");
                sb.EmitAppCall(scriptHash, "totalSupply");
                sb.EmitAppCall(scriptHash, "symbol");
                sb.EmitAppCall(scriptHash, "decimals");

                var result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
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
}
