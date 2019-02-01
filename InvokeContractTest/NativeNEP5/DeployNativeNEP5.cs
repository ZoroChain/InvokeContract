using System;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class DeployNativeNEP5 : IExample
    {
        public string Name => "DeployNativeNEP5 调用NativeNEP5的Deploy方法";

        public async Task StartAsync()
        {
            string ChainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string nativeNEP5Hash = Config.getValue("NativeNEP5");

            await DeployNativeNEP5Async(ChainHash, WIF, nativeNEP5Hash);
        }

        public async Task DeployNativeNEP5Async(string ChainHash, string WIF, string nativeNEP5Hash)
        {
            UInt160 nativeNEP5AssetId = ZoroHelper.Parse(nativeNEP5Hash);

            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Deploy", nativeNEP5AssetId);

                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, ChainHash, Config.GasPrice);
                Console.WriteLine(result);
            }
        }
    }
}
