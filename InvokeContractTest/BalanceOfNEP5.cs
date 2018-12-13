using System;
using System.Threading.Tasks;
using Zoro;
using Neo.VM;

namespace InvokeContractTest
{
    class BalanceOfNEP5 : IExample
    {
        public string Name => "BalanceOfNEP5 获取NEP5账户余额";

        public string ID => "3";

        public async Task StartAsync()
        {
            string WIF = Config.getValue("WIF");
            string ContractHash = Config.getValue("ContractHash");
            string[] ChainHashList = Config.getStringArray("ChainHashList");
            string nativeNEP5 = Config.getValue("NativeNEP5");
            UInt256.TryParse(nativeNEP5, out UInt256 nativeNEP5AssetId);
            UInt160 address = ZoroHelper.GetPublicKeyHashFromWIF(WIF);

            Console.WriteLine($"Account: {WIF}");
            if (Program.ChainID == "Zoro" && nativeNEP5AssetId != null)
            {
                await BalanceOfNativeNEP5(nativeNEP5AssetId, address, ChainHashList);
            }

            await BalanceOfNEP5Contract(ContractHash, address, ChainHashList);
        }

        async Task BalanceOfNativeNEP5(UInt256 nativeNEP5AssetId, UInt160 address, string[] chainHashList)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {                
                sb.EmitSysCall("Zoro.NativeNEP5.BalanceOf", nativeNEP5AssetId, address);
                sb.EmitSysCall("Zoro.NativeNEP5.Decimals", nativeNEP5AssetId);

                Console.WriteLine($"NativeNEP5: {nativeNEP5AssetId}");
                foreach (var chainHash in chainHashList)
                {
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    var value = GetBalanceFromJson(info);
                    string chainName = chainHash.Length > 0 ? chainHash : "Root";
                    Console.WriteLine($"balanceOf: {value}, chain:{chainName}");
                }
            }
        }

        async Task BalanceOfNEP5Contract(string NEP5Contract, UInt160 address, string[] chainHashList)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(ZoroHelper.Parse(NEP5Contract), "balanceOf", address);
                sb.EmitAppCall(ZoroHelper.Parse(NEP5Contract), "decimals");

                Console.WriteLine($"NEP5Contract: {NEP5Contract}");
                if (Program.ChainID == "Zoro")
                {
                    foreach (var chainHash in chainHashList)
                    {
                        var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                        var value = GetBalanceFromJson(info);
                        string chainName = chainHash.Length > 0 ? chainHash : "Root";
                        Console.WriteLine($"balanceOf: {value}, chain:{chainName}");
                    }
                }
                else
                {
                    string chainHash = UInt160.Zero.ToString();
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    var value = GetBalanceFromJson(info);
                    Console.WriteLine($"balanceOf: {value}");
                }
            }
        }

        string GetBalanceFromJson(string info)
        {
            string result = "";
            MyJson.JsonNode_Object json = MyJson.Parse(info) as MyJson.JsonNode_Object;

            if (json.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result = json["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result["stack"] as MyJson.JsonNode_Array;

                if (stack != null && stack.Count >= 2)
                {
                    string balance = ZoroHelper.GetJsonValue(stack[0] as MyJson.JsonNode_Object);
                    string decimals = ZoroHelper.GetJsonValue(stack[1] as MyJson.JsonNode_Object);
                    
                    Decimal value = Decimal.Parse(balance) / new Decimal(Math.Pow(10, int.Parse(decimals)));
                    string fmt = "{0:N" + decimals + "}";
                    result = string.Format(fmt, value);
                }
            }
            else if (json.ContainsKey("error"))
            {
                MyJson.JsonNode_Object json_error_obj = json["error"] as MyJson.JsonNode_Object;
                result = json_error_obj.ToString();
            }

            return result;
        }
    }
}
