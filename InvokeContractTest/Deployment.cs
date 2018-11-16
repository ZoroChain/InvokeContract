using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zoro;

namespace InvokeContractTest
{
    class Deployment : IExample
    {
        public string Name => "One Key Deployment 一键部署";

        public string ID => "7";

        public int waitTime = 3;

        public async Task StartAsync()
        {
            string[] ChainHashList = Config.getStringArray("ChainHashList");

            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");
            string ContractPath = Config.getValue("ContractPath");
            string ContractHash = Config.getValue("ContractHash");
            string transferValue = Config.getValue("transferValue");

            var createNep5 = Program.allExample["0"] as CreateNEP5;
            var deployNEP5 = Program.allExample["1"] as DeployNEP5;
            var invokeNEP5 = Program.allExample["2"] as InvokeNEP5;
            var TransferNEP5 = Program.allExample["4"] as TransferNEP5;

            foreach (var chainHash in ChainHashList)
            {
                await createNep5.CreateNep5Async(chainHash, WIF, ContractPath);
                Thread.Sleep(waitTime * 1000);

                await deployNEP5.DeployNEP5Async(chainHash, WIF, ContractHash);
                await invokeNEP5.InvokeNep5Async(chainHash, WIF, ContractHash);
                await TransferNEP5.TransferNEP5Async(chainHash, WIF, targetWIF, ContractHash, transferValue);
            }
        }
    }
}
