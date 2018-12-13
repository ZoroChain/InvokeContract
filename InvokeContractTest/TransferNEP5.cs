using System;
using System.Numerics;
using System.Threading.Tasks;
using Zoro;
using Zoro.Ledger;
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
            string nativeNEP5Hash = Config.getValue("NativeNEP5");

            Console.Write("转账金额：");
            string transferValue = Console.ReadLine();

            Console.Write("选择合约类型，0 - NativeNEP5, 1 - NEP5 SmartContract：");
            string nep5type = Console.ReadLine();

            if (nep5type == "0" || nep5type == "1")
            {
                Console.WriteLine($"From:{WIF}");
                Console.WriteLine($"To:{targetWIF}");
                Console.WriteLine($"Value:{transferValue}");
            }

            if (nep5type == "0")
            {
                Console.WriteLine($"Contract:{nativeNEP5Hash}");
                await TransferNativeNEP5(chainHash, WIF, targetWIF, nativeNEP5Hash, transferValue);
            }
            else if(nep5type == "1")
            {
                Console.WriteLine($"Contract:{contractHash}");
                await TransferNEP5Async(chainHash, WIF, targetWIF, contractHash, transferValue);
            }
        }

        public async Task TransferNEP5Async(string chainHash, string WIF, string targetWIF, string contractHash, string transferValue)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitAppCall(UInt160.Parse(contractHash), "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas + 1), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        public async Task TransferNativeNEP5(string chainHash, string WIF, string targetWIF, string contractHash, string transferValue)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.NativeNEP5.Transfer", UInt256.Parse(contractHash), scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }
    }
}
