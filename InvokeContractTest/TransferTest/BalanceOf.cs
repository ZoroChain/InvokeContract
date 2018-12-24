using System;
using System.Threading.Tasks;
using Zoro;
using Neo.VM;

namespace InvokeContractTest
{
    class BalanceOf : IExample
    {
        public string Name => "BalanceOf 获取账户余额";

        public async Task StartAsync()
        {
            string WIF = Config.getValue("WIF");
            string ContractHash = Config.getValue("ContractHash");
            string[] ChainHashList = Config.getStringArray("ChainHashList");
            string BCPHash = Config.getValue("BCPHash");
            string nativeNEP5Hash = Config.getValue("NativeNEP5");
            UInt160 address = ZoroHelper.GetPublicKeyHashFromWIF(WIF);

            Console.WriteLine($"Account: {WIF}");
            await BalanceOfBCP(BCPHash, address, ChainHashList);
            await BalanceOfNEP5Contract(ContractHash, address, ChainHashList);
            await BalanceOfNativeNEP5(nativeNEP5Hash, address, ChainHashList);
        }

        async Task BalanceOfBCP(string BCPHash, UInt160 address, string[] chainHashList)
        {
            UInt256 assetId = UInt256.Parse(BCPHash);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.GlobalAsset.BalanceOf", assetId, address);
                sb.EmitSysCall("Zoro.GlobalAsset.GetPrecision", assetId);

                foreach (var chainHash in chainHashList)
                {
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    var value = GetBalanceFromJson(info);
                    string chainName = chainHash.Length > 0 ? chainHash : "Root";
                    Console.WriteLine($"BalanceOf BCP:{value}, chain:{chainName}");
                }
            }
        }

        async Task BalanceOfNativeNEP5(string nativeNEP5Hash, UInt160 address, string[] chainHashList)
        {
            if (!UInt160.TryParse(nativeNEP5Hash, out UInt160 nativeNEP5AssetId))
                return;

            using (ScriptBuilder sb = new ScriptBuilder())
            {                
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "BalanceOf", nativeNEP5AssetId, address);
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Decimals", nativeNEP5AssetId);

                foreach (var chainHash in chainHashList)
                {
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    var value = GetBalanceFromJson(info);
                    string chainName = chainHash.Length > 0 ? chainHash : "Root";
                    Console.WriteLine($"BalanceOf NativeNEP5:{value}, chain:{chainName}");
                }
            }
        }

        async Task BalanceOfNEP5Contract(string NEP5Contract, UInt160 address, string[] chainHashList)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(ZoroHelper.Parse(NEP5Contract), "balanceOf", address);
                sb.EmitAppCall(ZoroHelper.Parse(NEP5Contract), "decimals");

                foreach (var chainHash in chainHashList)
                {
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    var value = GetBalanceFromJson(info);
                    string chainName = chainHash.Length > 0 ? chainHash : "Root";
                    Console.WriteLine($"BalanceOf NEP5:{value}, chain:{chainName}");
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
