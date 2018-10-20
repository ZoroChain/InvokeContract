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

        public string ChainHash = "";
        public string WIF = "";
        public string targetWIF = "";
        public string ContractPath = "";
        public string ContractHash = "";
        public string transferValue = "";

        public int waitTime = 10;

        public async Task StartAsync()
        {
            Console.WriteLine("Params:ChainHash,WIF,targetWIF,ContractPath,ContractHash,transferValue");
            var param = Console.ReadLine();
            string[] messages = param.Split(",");
            Console.WriteLine("ChainHash:{0}, WIF:{1}, targetWIF:{2}, ContractPath:{3}, ContractHash:{4}, transferValue:{5}", messages[0], messages[1], messages[2], messages[3], messages[4], messages[5]);
            ChainHash = messages[0] == ""?Fixed8.Zero.ToString(): messages[0];
            WIF = messages[1] == "" ? "L1PSC3LRShi51xHAX2KN9oCFqETrZQhnzhKVu5zbrzdDpxF1LQz3" : messages[1];
            targetWIF = messages[2] == "" ? "L17Cq1FEbZJ8bc8Y8HcqVCgxsNpWY6LHDoau9DBD98m8vtGcVpuQ" : messages[2];
            ContractPath = messages[3] == "" ? "BcpContract.avm" : messages[3];
            ContractHash = messages[4] == "" ? "c4108917282bff79b156d4d01315df811790c0e8" : messages[4];
            transferValue = messages[5] == "" ? "10000000000" : messages[5];

            var createNep5 = Program.allExample["0"] as CreateNEP5;
            await createNep5.CreateNep5Async(ChainHash, WIF, ContractPath);

            Thread.Sleep(waitTime * 1000);

            var deployNEP5 = Program.allExample["1"] as DeployNEP5;
            await deployNEP5.DeployNEP5Async(ChainHash, WIF, ContractHash);

            Thread.Sleep(waitTime * 1000);

            var TransferNEP5 = Program.allExample["2"] as TransferNEP5;
            await TransferNEP5.TransferNEP5Async(ChainHash, WIF, targetWIF, ContractHash, transferValue);
        }
    }
}
