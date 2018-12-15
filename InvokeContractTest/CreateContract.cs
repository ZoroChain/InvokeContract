using Neo.VM;
using System;
using Zoro;
using Zoro.SmartContract;
using System.Threading.Tasks;
using Zoro.Wallets;

namespace InvokeContractTest
{
    class CreateContract : IExample
    {
        public string Name => "CreateContract 发布一个测试合约";

        public string ID => "14";

        public async Task StartAsync()
        {
            string ChainHash = Config.getValue("ChainHash");
            string WIF = Config.getValue("WIF");
            string ContractPath = "TestContract.avm";

            await CreateContractAsync(ChainHash, WIF, ContractPath);
        }

        public async Task CreateContractAsync(string ChainHash, string WIF, string ContractPath)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);

            byte[] script = System.IO.File.ReadAllBytes(ContractPath);
            Console.WriteLine("合约脚本Hash：" + script.ToScriptHash());
            byte[] parameter__list = ZoroHelper.HexString2Bytes("0710");
            byte[] return_type = ZoroHelper.HexString2Bytes("05");
            int need_storage = 0;
            int need_nep4 = 0;
            int need_canCharge = 4;
            string name = "test contract";
            string version = "1.0";
            string auther = "zz";
            string email = "0";
            string description = "0";
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                var ss = need_storage | need_nep4 | need_canCharge;
                sb.EmitPush(description);
                sb.EmitPush(email);
                sb.EmitPush(auther);
                sb.EmitPush(version);
                sb.EmitPush(name);
                sb.EmitPush(ss);
                sb.EmitPush(return_type);
                sb.EmitPush(parameter__list);
                sb.EmitPush(script);
                sb.EmitSysCall("Neo.Contract.Create");

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), ChainHash);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, ChainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }
    }
}
