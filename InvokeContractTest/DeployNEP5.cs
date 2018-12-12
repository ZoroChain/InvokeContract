using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class DeployNEP5 : IExample
    {
        public string Name => "deploy 调用nep5合约中的deploy方法";

        public string ID => "1";

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

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), ChainHash);

                await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, ChainHash, Fixed8.FromDecimal(gas), Config.GasPrice);
            }
        }
    }
}
