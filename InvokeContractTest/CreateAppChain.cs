using System;
using System.Linq;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Zoro.Cryptography;
using Neo.VM;

namespace InvokeContractTest
{
    class CreateAppChain : IExample
    {
        public string Name => "CreateAppChain 创建应用链";

        public string ID => "8";

        public async Task StartAsync()
        {
            Console.WriteLine("AppChainName:");
            string name = Console.ReadLine();
            string WIF = Config.getValue("WIF");

            Console.Write("validators Length: ");
            string vlength = Console.ReadLine();
            string[] validators = new string[int.Parse(vlength)];
            for (int i = 0; i < validators.Length; i++) {
                Console.Write("validator " + (i + 1) + ": ");
                validators[i] = Console.ReadLine();
            }
            Console.Write("seedList Length: ");
            string slength = Console.ReadLine();
            string[] seedList = new string[int.Parse(slength)];
            for (int i = 0; i < seedList.Length; i++) {
                Console.Write("seed " + (i + 1) + ": ");
                seedList[i] = Console.ReadLine();
            }

            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                foreach (string validator in validators)
                {
                    sb.EmitPush(validator);
                }
                sb.EmitPush(validators.Length);
                foreach (string seed in seedList)
                {
                    sb.EmitPush(seed);
                }
                sb.EmitPush(seedList.Length);
                sb.EmitPush(DateTime.UtcNow.ToTimestamp());
                sb.EmitPush(keypair.PublicKey.EncodePoint(true));
                sb.EmitPush(name);

                UInt160 chainHash = new UInt160(Crypto.Default.Hash160(sb.ToArray()));
                sb.EmitPush(chainHash.ToArray());
                sb.EmitSysCall("Zoro.AppChain.Create");

                Console.WriteLine("Appchain hash:" + chainHash.ToArray().Reverse().ToHexString());

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), "");

                string result = await ZoroHelper.SendRawTransaction(sb.ToArray(), keypair, "", Fixed8.FromDecimal(gas), Config.GasPrice);
                Console.WriteLine(result);
            }
        }
    }
}
