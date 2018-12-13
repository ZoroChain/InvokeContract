using System;
using System.Numerics;
using System.Globalization;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class TransferNEP5 : IExample
    {
        public string Name => "TransferNEP5 进行一次NEP5转账交易";

        public string ID => "4";

        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            string contractHash = Config.getValue("ContractHash");
            string nativeNEP5AssetId = Config.getValue("NativeNEP5");

            Console.Write("Choose Transaction Type，0 - NativeNEP5, 1 - NEP5 SmartContract, 2 - ContractTransaction：");
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
                Console.WriteLine($"AssetId:{nativeNEP5AssetId}");

                byte decimals = await GetNativeNEP5Decimals(nativeNEP5AssetId, chainHash);
                Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

                await TransferNativeNEP5(chainHash, WIF, targetWIF, nativeNEP5AssetId, new BigInteger(value));
            }
            else if(transType == 1)
            {
                Console.WriteLine($"Contract Hash:{contractHash}");

                byte decimals = await GetNEP5Decimals(contractHash, chainHash);
                Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

                await TransferNEP5Async(chainHash, WIF, targetWIF, contractHash, new BigInteger(value));
            }
            else if (transType == 2)
            {
                Console.WriteLine($"AssetId:{nativeNEP5AssetId}");

                byte decimals = await GetNativeNEP5Decimals(nativeNEP5AssetId, chainHash);
                Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

                KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
                UInt160 targetAddress = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);
                UInt256 assetId = UInt256.Parse(nativeNEP5AssetId);

                BigInteger bigValue = new BigInteger(value);
                await ZoroHelper.SendContractTransaction(assetId, keypair, targetAddress, new Fixed8((long)bigValue), chainHash, Config.GasPrice);
            }
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

        public async Task TransferNativeNEP5(string chainHash, string WIF, string targetWIF, string assetId, BigInteger value)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.NativeNEP5.Transfer", UInt256.Parse(assetId), scriptHash, targetscripthash, value);

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
                sb.EmitSysCall("Zoro.NativeNEP5.Decimals", UInt256.Parse(assetId));

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
