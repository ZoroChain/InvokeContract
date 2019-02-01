using System;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class DeployNEP5 : IExample
    {
        public string Name => "DeployNEP5 调用nep5合约中的deploy方法";

        public async Task StartAsync()
        {
            string ChainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string ContractHash = Config.getValue("ContractHash");

            await DeployNEP5Async(ChainHash, WIF, ContractHash);
        }

        public async Task DeployNEP5Async(string ChainHash, string WIF, string ContractHash) {

            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(ZoroHelper.Parse(ContractHash), "deploy", "1");

                string result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, ChainHash, Config.GasPrice);
                Console.WriteLine(result);
            }
        }
    }
}
