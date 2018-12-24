using System;
using System.Numerics;
using System.Globalization;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class TransferTest : IExample
    {
        public string Name => "TransferTest 进行一次转账交易";

        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            string contractHash = Config.getValue("ContractHash");
            string BCPHash = Config.getValue("BCPHash");
            string nativeNEP5Hash = Config.getValue("NativeNEP5");

            Console.Write("Choose Transaction Type，0 - NEP5 SmartContract, 1 - NativeNEP5, 2 - BCP：");
            int transType = int.Parse(Console.ReadLine());

            Console.Write("Transfer Amount:");
            string transferValue = Console.ReadLine();

            if (transType == 0 || transType == 1 || transType == 2)
            {
                Console.WriteLine($"From:{WIF}");
                Console.WriteLine($"To:{targetWIF}");
                Console.WriteLine($"Value:{transferValue}");
            }

            if (transType == 0)
            {
                await TransferNEP5(nativeNEP5Hash, chainHash, transferValue, WIF, targetWIF);
            }
            else if(transType == 1)
            {
                await TransferNativeNEP5(nativeNEP5Hash, chainHash, transferValue, WIF, targetWIF);
            }
            else if (transType == 2)
            {
                await TransferGlobalAsset(BCPHash, chainHash, transferValue, WIF, targetWIF);
            }
        }

        public async Task TransferNEP5(string contractHash, string chainHash, string transferValue, string WIF, string targetWIF)
        {
            Console.WriteLine($"NEP5 Contract Hash:{contractHash}");

            byte decimals = await GetNativeNEP5Decimals(contractHash, chainHash);
            Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

            await TransferNEP5Async(chainHash, WIF, targetWIF, contractHash, new BigInteger(value));
        }

        public async Task TransferNativeNEP5(string nativeNEP5Hash, string chainHash, string transferValue, string WIF, string targetWIF)
        {
            Console.WriteLine($"NativeNEP5 AssetId:{nativeNEP5Hash}");

            byte decimals = await GetNativeNEP5Decimals(nativeNEP5Hash, chainHash);
            Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

            await TransferNativeNEP5Async(chainHash, WIF, targetWIF, nativeNEP5Hash, new BigInteger(value));
        }

        public async Task TransferGlobalAsset(string assetId, string chainHash, string transferValue, string WIF, string targetWIF)
        {
            Console.WriteLine($"Global AssetId:{assetId}");

            byte decimals = await GetGlobalAssetDecimals(assetId, chainHash);
            Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

            await TransferGlobalAssetAsync(chainHash, WIF, targetWIF, assetId, new BigInteger(value));
        }

        public async Task TransferNEP5Async(string chainHash, string WIF, string targetWIF, string contractHash, BigInteger value)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitAppCall(UInt160.Parse(contractHash), "transfer", scriptHash, targetscripthash, value);

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                gas = Math.Max(Config.GasNEP5Transfer, gas);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        public async Task TransferNativeNEP5Async(string chainHash, string WIF, string targetWIF, string assetId, BigInteger value)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Transfer", UInt160.Parse(assetId), scriptHash, targetscripthash, value);

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        public async Task TransferGlobalAssetAsync(string chainHash, string WIF, string targetWIF, string assetId, BigInteger value)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.GlobalAsset.Transfer", UInt256.Parse(assetId), scriptHash, targetscripthash, value);

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        async Task<byte> GetNEP5Decimals(string contractHash, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(UInt160.Parse(contractHash), "decimals");

                var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);

                return ParseDecimals(info);
            }
        }

        async Task<byte> GetNativeNEP5Decimals(string assetId, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Decimals", UInt160.Parse(assetId));

                var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);

                return ParseDecimals(info);
            }
        }

        async Task<byte> GetGlobalAssetDecimals(string assetId, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.GlobalAsset.Decimals", UInt256.Parse(assetId));

                var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);

                return ParseDecimals(info);
            }
        }

        byte ParseDecimals(string info)
        {
            byte decimals = 0;

            MyJson.JsonNode_Object json = MyJson.Parse(info) as MyJson.JsonNode_Object;

            if (json.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result = json["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array json_stack = json_result["stack"] as MyJson.JsonNode_Array;

                if (json_stack != null && json_stack.Count >= 1)
                {
                    string value = ZoroHelper.GetJsonValue(json_stack[0] as MyJson.JsonNode_Object);
                    decimals = byte.Parse(value);
                }
            }
            return decimals;
        }
    }
}
