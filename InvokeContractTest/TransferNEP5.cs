using System;
using System.Numerics;
using System.Threading.Tasks;
using Zoro;
using Zoro.Cryptography.ECC;
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

            await TransferNEP5Async(chainHash, WIF, targetWIF, contractHash, transferValue);
        }

        public async Task TransferNEP5Async(string chainHash, string WIF, string targetWIF, string contractHash, string transferValue) {

            byte[] prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            ECPoint pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

            byte[] tragetprikey = ZoroHelper.GetPrivateKeyFromWIF(targetWIF);
            ECPoint targetpubkey = ZoroHelper.GetPublicKeyFromPrivateKey(tragetprikey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHash(targetpubkey);

            Console.WriteLine($"From:{WIF.ToString()}");
            Console.WriteLine($"To:{targetWIF.ToString()}");
            Console.WriteLine($"Value:{transferValue}");

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitAppCall(ZoroHelper.Parse(contractHash), "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), scriptHash, prikey, pubkey, chainHash);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }

        }
    }
}
