using Neo.VM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using System.Numerics;
using System.Globalization;

namespace InvokeContractTest
{
    public class ContractTest : IExample
    {
        public string Name => "CreateTest 测试合约接口";

        public async Task StartAsync()
        {
            string[] ChainHashList = Config.getStringArray("ChainHashList");
            string ChainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string contractHash = Config.getValue("TestContract");
            UInt160 address = ZoroHelper.GetPublicKeyHashFromWIF(WIF);
            var nativeBcp = UInt256.Parse(Config.getValue("NativeNEP5"));
            Console.WriteLine("Choose Transaction Type，0 - Contract Invoke NativeNEP5, 1 -  Contract Transfer NativeNEP5, 2 -  Contract Transfer_App NativeNEP5, 3 - TestContract Balance:");
            int transType = int.Parse(Console.ReadLine());
            if (transType == 0)
                await ContractInvokeTestAsync(contractHash, address, nativeBcp);
            if (transType == 1)
                await SendRawTransactionTestAsync(contractHash, WIF, nativeBcp,transType);
            if (transType == 2)
                await SendRawTransactionTestAsync(contractHash, WIF, nativeBcp,transType);
            if (transType == 3)
                await BalanceOfNativeNEP5(nativeBcp, UInt160.Parse(contractHash), ChainHashList);
        }

        public async Task ContractInvokeTestAsync(string contractHash, UInt160 address, UInt256 nativeBcp)
        {
            string[] ChainHashList = Config.getStringArray("ChainHashList");
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "balanceOf", nativeBcp, address);
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "symbol", nativeBcp);
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "decimals", nativeBcp);
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "totalSupply", nativeBcp);
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "test");
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "call");
                //sb.EmitAppCall(ZoroHelper.Parse(contractHash), "getheight");
                sb.EmitAppCall(ZoroHelper.Parse(contractHash), "getheader", 20);

                Console.WriteLine($"Contract: {contractHash}");

                foreach (var chainHash in ChainHashList)
                {
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    string chainName = chainHash.Length > 0 ? chainHash : "Root";
                    Console.WriteLine($"{info}");
                }
            }
        }

        public async Task SendRawTransactionTestAsync(string contractHash,string WIF, UInt256 nativeBcp, int transType)
        {
            Console.WriteLine("Transfer Amount:");
            string transferValue = Console.ReadLine();
            Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, 8));
            string chainHash = Config.getValue("ChainHash");
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = UInt160.Parse(contractHash);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);
                if (transType == 1)
                    sb.EmitAppCall(ZoroHelper.Parse(contractHash), "transfer", nativeBcp, scriptHash, targetscripthash, (BigInteger)value);
                if(transType==2)
                    sb.EmitAppCall(ZoroHelper.Parse(contractHash), "transfer_app", nativeBcp, scriptHash, (BigInteger)value);
                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas + 1), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        async Task BalanceOfNativeNEP5(UInt256 nativeNEP5AssetId, UInt160 address, string[] chainHashList)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "BalanceOf", nativeNEP5AssetId, address);
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Decimals", nativeNEP5AssetId);

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
