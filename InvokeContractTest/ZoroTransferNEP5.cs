using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Zoro;
using Zoro.IO;
using Zoro.Ledger;
using Zoro.Network.P2P.Payloads;
using Neo.VM;

namespace InvokeContractTest
{
    class ZoroTransferNEP5 : IExample
    {
        public string Name => "transfer 进行一次nep5合约交易";

        public string ID => "4";

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
            ChainHash = Config.getValue("ChainHash");
            WIF = Config.getValue("WIF");
            targetWIF = Config.getValue("targetWIF");
            ContractHash = Config.getValue("ContractHash");
            transferValue = Config.getValue("transferValue");

            await TransferNEP5Async(ChainHash, WIF, targetWIF, ContractHash, transferValue);
        }

        public async Task TransferNEP5Async(string ChainHash, string WIF, string targetWIF, string ContractHash, string transferValue) {

            byte[] prikey = ZoroHelper.GetPrivateKeyFromWIF(WIF);
            Zoro.Cryptography.ECC.ECPoint pubkey = ZoroHelper.GetPublicKeyFromPrivateKey(prikey);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(pubkey);

            byte[] tragetprikey = ZoroHelper.GetPrivateKeyFromWIF(targetWIF);
            Zoro.Cryptography.ECC.ECPoint targetpubkey = ZoroHelper.GetPublicKeyFromPrivateKey(tragetprikey);
            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHash(targetpubkey);

            Console.WriteLine(WIF.ToString());
            Console.WriteLine(targetWIF.ToString());

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                byte[] randomBytes = new byte[32];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                BigInteger randomNum = new BigInteger(randomBytes);
                sb.EmitPush(randomNum);
                sb.EmitPush(Neo.VM.OpCode.DROP);
                sb.EmitAppCall(ZoroHelper.Parse(ContractHash), "transfer", scriptHash, targetscripthash, BigInteger.Parse(transferValue));

                InvocationTransaction tx = new InvocationTransaction
                {
                    ChainHash = ZoroHelper.Parse(ChainHash),
                    Version = 1,
                    Script = sb.ToArray(),
                    Gas = Fixed8.Zero,
                };

                tx.Inputs = new CoinReference[0];
                tx.Outputs = new TransactionOutput[0];

                tx.Attributes = new TransactionAttribute[1];
                tx.Attributes[0] = new TransactionAttribute();
                tx.Attributes[0].Usage = TransactionAttributeUsage.Script;
                tx.Attributes[0].Data = scriptHash.ToArray();

                byte[] data = ZoroHelper.GetHashData(tx);
                byte[] signdata = ZoroHelper.Sign(data, prikey, pubkey);
                ZoroHelper.AddWitness(tx, signdata, pubkey);
                string rawdata = ThinNeo.Helper.Bytes2HexString(tx.ToArray());

                string url;
                byte[] postdata;
                if (ChainHash.Length > 0)
                {
                    MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                    postRawArray.AddArrayValue(ChainHash);
                    postRawArray.AddArrayValue(rawdata);

                    url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
                }
                else
                {
                    url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
                }

                var result = await Helper.HttpPost(url, postdata);
                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }

        }
    }
}
