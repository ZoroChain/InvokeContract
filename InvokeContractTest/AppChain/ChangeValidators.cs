using System;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class ChangeValidators : IExample
    {
        public string Name => "ChangeValidators 更改应用链共识节点";

        public async Task StartAsync()
        {
            Console.WriteLine("AppChainHash:");
            string appchainHash = Console.ReadLine();

            var url = Helper.MakeRpcUrl(Program.local, "getappchainstate", new MyJson.JsonNode_ValueString(appchainHash));
            var result = await Helper.HttpGet(url);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj;

            bool exists = true;
            if (json_result_array.TryGetValue("result", out MyJson.IJsonNode json_result))
            {
                json_result_obj = json_result as MyJson.JsonNode_Object;

                string appchainName = json_result_obj["name"].AsString();

                if (appchainName == "null")
                {
                    exists = false;
                }
            }
            else
            {
                exists = false;
            }

            if (!exists)
            {
                Console.WriteLine("Can't find appchain " + appchainHash);
                return;
            }

            string WIF = Config.getValue("WIF");

            Console.Write("validators Length: ");
            string vlength = Console.ReadLine();
            string[] validators = new string[int.Parse(vlength)];
            for (int i = 0; i < validators.Length; i++)
            {
                Console.Write("validator " + (i + 1) + ": ");
                validators[i] = Console.ReadLine();
            }

            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                foreach (string validator in validators)
                {
                    sb.EmitPush(validator);
                }
                sb.EmitPush(validators.Length);
                sb.EmitSysCall("Zoro.AppChain.ChangeValidators");

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), appchainHash);

                result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, appchainHash, Fixed8.FromDecimal(gas), Config.GasPrice);
                Console.WriteLine(result);
            }
                
        }
    }
}
