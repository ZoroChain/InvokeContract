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
        public string Name => "transfer 进行一次nep5合约交易";

        public string ID => "4";

        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            string contractHash = Config.getValue("ContractHash");
            string transferValue = Config.getValue("transferValue");
            string nativeNEP5 = Config.getValue("NativeNEP5");

            await TransferNEP5Async(chainHash, WIF, targetWIF, contractHash, transferValue, UInt256.TryParse(nativeNEP5, out UInt256 assetId) ? assetId : null);
        }

        public async Task TransferNEP5Async(string chainHash, string WIF, string targetWIF, string contractHash, string transferValue, UInt256 nativeNEP5 = null)
        {

            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);

            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            Console.WriteLine($"From:{WIF.ToString()}");
            Console.WriteLine($"To:{targetWIF.ToString()}");
            Console.WriteLine($"Value:{transferValue}");

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                if (nativeNEP5 != null)
                {
                    sb.EmitSysCall("Zoro.NativeNEP5.Transfer", nativeNEP5, scriptHash, targetscripthash, BigInteger.Parse(transferValue));
                }
                else
                {
                    sb.EmitAppCall(ZoroHelper.Parse(contractHash), "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));
                }

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }

        }
    }
}
