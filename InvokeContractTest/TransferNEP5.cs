using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThinNeo;

namespace InvokeContractTest
{
    class TransferNEP5 : IExample
    {
        public string Name => "transfer 进行一次nep5合约交易";

        public string ID => "2";

        private string chainHash;
        private string wif;
        private string targetwif;
        private string contractHash;
        public string ChainHash { get => chainHash; set => chainHash = value; }
        public string WIF { get => wif; set => wif = value; }
        public string targetWIF { get => targetwif; set => targetwif = value; }
        public string ContractHash { get => contractHash; set => contractHash = value; }
        public string transferValue;

        public async Task StartAsync()
        {
            Console.WriteLine("Params:ChainHash,WIF,targetWIF,ContractHash,transferValue");
            var param = Console.ReadLine();
            string[] messages = param.Split(",");
            Console.WriteLine("ChainHash:{0}, WIF:{1}, targetWIF:{2}, ContractPath:{3}, transferValue:{4}", messages[0], messages[1], messages[2], messages[3], messages[4]);
            ChainHash = messages[0];
            WIF = messages[1];
            targetWIF = messages[2];
            ContractHash = messages[3];
            transferValue = messages[4];

            await TransferNEP5Async(ChainHash, WIF, targetWIF, ContractHash, transferValue);
        }

        public async Task TransferNEP5Async(string ChainHash, string WIF, string targetWIF, string ContractHash, string transferValue) {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            var scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);

            byte[] tragetprikey = ThinNeo.Helper.GetPrivateKeyFromWIF(targetWIF);
            byte[] targetpubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(tragetprikey);
            string targetAddress = ThinNeo.Helper.GetAddressFromPublicKey(targetpubkey);
            var targetscripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetAddress);

            //ThreadPool.QueueUserWorkItem(async (p) => {
            //    while (true)
            //    {
            using (ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                byte[] randomBytes = new byte[32];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                BigInteger randomNum = new BigInteger(randomBytes);
                sb.EmitPushNumber(randomNum);
                sb.Emit(ThinNeo.VM.OpCode.DROP);
                array.AddArrayValue("(addr)" + address);//from
                array.AddArrayValue("(addr)" + targetAddress);//to
                array.AddArrayValue("(int)" + transferValue);//value
                sb.EmitParamJson(array);
                sb.EmitPushString("transfer");
                sb.EmitAppCall(new Hash160(ContractHash));

                string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

                MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                postArray.AddArrayValue(ChainHash);
                postArray.AddArrayValue(scriptPublish);

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
                postRawArray.AddArrayValue(ChainHash);
                postRawArray.AddArrayValue(rawdata);

                byte[] postdata;
                var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                var result = await Helper.HttpPost(url, postdata);
                Console.WriteLine(address + " " + targetAddress + "  " + transferValue);
            }
            //    }
            //});
        }
    }
}
