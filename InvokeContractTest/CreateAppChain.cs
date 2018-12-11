using System;
using System.Linq;
using System.Threading.Tasks;
using Zoro;
using Zoro.Network.P2P.Payloads;
using Zoro.Cryptography;
using Zoro.Cryptography.ECC;
using Neo.VM;

namespace InvokeContractTest
{
    class CreateAppChain : IExample
    {
        public string Name => "createAppChain 创建应用链";

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

            byte[] prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            ECPoint pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

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
                sb.EmitPush(pubkey.EncodePoint(true));
                sb.EmitPush(name);

                UInt160 chainHash = new UInt160(Crypto.Default.Hash160(sb.ToArray()));
                sb.EmitPush(chainHash.ToArray());
                sb.EmitSysCall("Zoro.AppChain.Create");

                Console.WriteLine("Appchain hash:" + chainHash.ToArray().Reverse().ToHexString());

                string result = await ZoroHelper.SendRawTransaction(sb.ToArray(), scriptHash, prikey, pubkey, "");
                Console.WriteLine(result);
            }
        }
    }
}
