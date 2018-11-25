using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;
using Zoro;
using Zoro.Cryptography;
using Zoro.Cryptography.ECC;

namespace InvokeContractTest
{
    class CreateAppChain:IExample
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

            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            ScriptBuilder sb = new ScriptBuilder();
            foreach (string validator in validators) {
                sb.EmitPushString(validator);
            }
            sb.EmitPushNumber(validators.Length);
            foreach (string seed in seedList)
            {
                sb.EmitPushString(seed);
            }
            sb.EmitPushNumber(seedList.Length);
            sb.EmitPushNumber(DateTime.UtcNow.ToTimestamp());
            sb.EmitPushBytes(ECPoint.FromBytes(pubkey, ECCurve.Secp256r1).EncodePoint(true));
            sb.EmitPushString(name);

            UInt160 chainHash = new UInt160(Crypto.Default.Hash160(sb.ToArray()));
            sb.EmitPushBytes(chainHash.ToArray());
            sb.EmitSysCall("Zoro.AppChain.Create");

            Console.WriteLine("Appchain hash:" + chainHash.ToArray().Reverse().ToHexString());

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            byte[] postdata;

            ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
            extdata.script = sb.ToArray();

            //extdata.gas = Math.Ceiling(gas_consumed - 10);
            extdata.gas = 0;

            ThinNeo.Transaction tran = Helper.makeTran(null, null, new ThinNeo.Hash256(Program.id_GAS), extdata.gas);
            tran.version = 1;
            tran.extdata = extdata;
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            //附加鉴证
            tran.attributes = new ThinNeo.Attribute[1];
            tran.attributes[0] = new ThinNeo.Attribute();
            tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
            tran.attributes[0].data = scripthash;

            byte[] msg = tran.GetMessage();
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
            postRawArray.AddArrayValue("");
            postRawArray.AddArrayValue(rawdata);

            var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);
            Console.WriteLine(result);
        }
    }
}
