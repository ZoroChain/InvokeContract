using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Security.Cryptography;
using ThinNeo;
using Zoro;

namespace InvokeContractTest
{
    class ChangeSeedList : IExample
    {
        public string Name => "ChangeSeedList 更改应用链种子节点地址";

        public string ID => "9";

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

            Console.Write("seedList Length: ");
            string slength = Console.ReadLine();
            string[] seedList = new string[int.Parse(slength)];
            for (int i = 0; i < seedList.Length; i++)
            {
                Console.Write("seed " + (i + 1) + ": ");
                seedList[i] = Console.ReadLine();
            }

            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
            UInt160 chainHash = UInt160.Parse(appchainHash);

            ScriptBuilder sb = new ScriptBuilder();
            byte[] randomBytes = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            BigInteger randomNum = new BigInteger(randomBytes);
            sb.EmitPushNumber(randomNum);
            sb.Emit(ThinNeo.VM.OpCode.DROP);
            foreach (string seed in seedList)
            {
                sb.EmitPushString(seed);
            }
            sb.EmitPushNumber(seedList.Length);
            sb.EmitSysCall("Zoro.AppChain.ChangeSeedList");
            
            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

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
            postRawArray.AddArrayValue(appchainHash);
            postRawArray.AddArrayValue(rawdata);

            byte[] postdata;
            url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            result = await Helper.HttpPost(url, postdata);
            Console.WriteLine(result);
        }
    }
}
