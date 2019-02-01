using System;
using System.Numerics;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Zoro.SmartContract;
using Neo.VM;

namespace InvokeContractTest
{
    class CreateNativeNEP5 : IExample
    {
        public string Name => "CreateNativeNEP5 创建NativeNEP5";

        public async Task StartAsync()
        {
            string WIF = Config.getValue("WIF");
            string ChainHash = Config.getValue("ChainHash");

            await CreateNativeNEP5Async(ChainHash, WIF);
        }

        public async Task<UInt160> CreateNativeNEP5Async(string ChainHash, string WIF)
        {
            string symbol = "InvokeContractTest";
            string name = "InvokeContractTest_NativeNEP5";

            BigInteger presion = new BigInteger(8);
            Decimal totalsupply = 2_000_000_000;
            Decimal amount = totalsupply * new Decimal(Math.Pow(10, (long)presion));

            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);

            UInt160 hash = new UInt160();

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(scriptHash);
                sb.EmitPush(keypair.PublicKey.EncodePoint(true));
                sb.EmitPush(presion);
                sb.EmitPush(new BigInteger(amount));
                sb.EmitPush(symbol);
                sb.EmitPush(name);
                sb.EmitSysCall("Zoro.NativeNEP5.Create");

                hash = sb.ToArray().ToScriptHash();
                Console.WriteLine("NativeNEP5 Hash:" + hash);

                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, ChainHash, Config.GasPrice);
                Console.WriteLine(result);
            }

            return hash;
        }
    }
}
