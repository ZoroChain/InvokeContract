using System.Threading;
using System.Threading.Tasks;
using Zoro;

namespace InvokeContractTest
{
    class Deployment : IExample
    {
        public string Name => "One Key Deployment 一键部署NEP5测试合约";

        public int waitTime = 3;

        public async Task StartAsync()
        {
            string[] ChainHashList = Config.getStringArray("ChainHashList");

            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            string ContractPath = Config.getValue("ContractPath");
            string ContractHash = Config.getValue("ContractHash");
            string nativeNEP5Hash = Config.getValue("NativeNEP5");

            var createNep5 = Program.allExample["11"] as CreateNEP5;
            var deployNEP5 = Program.allExample["12"] as DeployNEP5;
            var invokeNEP5 = Program.allExample["13"] as InvokeNEP5;

            var createNativeNep5 = Program.allExample["14"] as CreateNativeNEP5;
            var deployNativeNEP5 = Program.allExample["15"] as DeployNativeNEP5;
            var invokeNativeNEP5 = Program.allExample["16"] as InvokeNativeNEP5;

            foreach (var chainHash in ChainHashList)
            {
                await createNep5.CreateNep5Async(chainHash, WIF, ContractPath);
                Thread.Sleep(waitTime * 1000);
                await deployNEP5.DeployNEP5Async(chainHash, WIF, ContractHash);
                Thread.Sleep(waitTime * 1000);
                await invokeNEP5.InvokeNep5Async(chainHash, WIF, ContractHash);

                await createNativeNep5.CreateNativeNEP5Async(chainHash, WIF);
                Thread.Sleep(waitTime * 1000);
                await deployNativeNEP5.DeployNativeNEP5Async(chainHash, WIF, nativeNEP5Hash);
                Thread.Sleep(waitTime * 1000);
                await invokeNativeNEP5.InvokeNativeNep5Async(chainHash, nativeNEP5Hash);
            }
        }
    }
}
