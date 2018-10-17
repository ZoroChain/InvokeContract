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
    class CreateAppChain
    {
        public string[] validators = new string[] {   "02e42b7e95cd9d91d8b473a340b1c2827d50715f9e95ce003587dc29513499cb08",
                                                      "03b8d429f0d57a8cdf93dc7faf850fef23d2a17ce669bba7640cd0bcda21eafb0b",
                                                      "02da01221b5d9064305ababd48bea8fadbd09ec2f1a4e674d470de80ce345cc356",
                                                      "031f210b12e522205295cec1e28e1420cb581d1235157015a1dbb033586530b704"};
        public string[] seedList = new string[] { "10.1.6.227:32001",
                                                  "10.1.6.227:32002",
                                                  "10.1.6.227:32003",
                                                  "10.1.6.227:32004"};
        public string name = "New appChain";
        public string WIF = "L1PSC3LRShi51xHAX2KN9oCFqETrZQhnzhKVu5zbrzdDpxF1LQz3";

        public async void Start() {
            await StartAsync();
        }

        public async Task StartAsync() {

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
            sb.EmitPushString(chainHash.ToString());
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
            postRawArray.AddArrayValue("0");
            postRawArray.AddArrayValue(rawdata);

            var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);
        }
    }
}
