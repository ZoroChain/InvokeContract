using System.Threading.Tasks;
using Zoro;
using Zoro.Network.P2P.Payloads;
using Zoro.Cryptography.ECC;
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

            byte[] prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            ECPoint pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(ZoroHelper.Parse(ContractHash), "deploy", "1");

                await ZoroHelper.SendRawTransaction(sb.ToArray(), scriptHash, prikey, pubkey, ChainHash);
            }
        }
    }
}
