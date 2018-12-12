using Zoro;
using Zoro.IO;
using Zoro.Wallets;
using Zoro.SmartContract;
using Zoro.Cryptography;
using Zoro.Cryptography.ECC;
using Zoro.Network.P2P.Payloads;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo.VM;

namespace InvokeContractTest
{
    class ZoroHelper
    {
        public static UInt160 Parse(string value)
        {
            if (value.StartsWith("0x"))
                value = value.Substring(2);
            if (value.Length != 40)
                return UInt160.Zero;
            return new UInt160(value.HexToBytes().Reverse().ToArray());
        }

        public static KeyPair GetKeyPairFromWIF(string wif)
        {
            byte[] prikey = Wallet.GetPrivateKeyFromWIF(wif);
            KeyPair keypair = new KeyPair(prikey);
            return keypair;
        }

        public static byte[] GetPrivateKeyFromWIF(string wif)
        {
            byte[] prikey = Wallet.GetPrivateKeyFromWIF(wif);
            return prikey;
        }

        public static ECPoint GetPublicKeyFromPrivateKey(byte[] prikey)
        {
            ECPoint pubkey = ECCurve.Secp256r1.G * prikey;
            return pubkey;
        }

        public static UInt160 GetPublicKeyHash(ECPoint pubkey)
        {
            UInt160 script_hash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            return script_hash;
        }

        public static UInt160 GetPublicKeyHashFromWIF(string WIF)
        {
            byte[] prikey = GetPrivateKeyFromWIF(WIF);
            ECPoint pubkey = GetPublicKeyFromPrivateKey(prikey);
            return GetPublicKeyHash(pubkey);
        }

        public static byte[] Sign(byte[] data, byte[] prikey, ECPoint pubkey)
        {
            return Crypto.Default.Sign(data, prikey, pubkey.EncodePoint(false).Skip(1).ToArray());
        }

        public static byte[] GetHashData(IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] GetRawData(IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.Serialize(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static void AddWitness(Transaction tx, byte[] signdata, ECPoint pubkey)
        {
            var vscript = Contract.CreateSignatureRedeemScript(pubkey).ToArray();

            var sb = new ThinNeo.ScriptBuilder();
            sb.EmitPushBytes(signdata);

            var iscript = sb.ToArray();

            AddWitness(tx, vscript, iscript);
        }

        public static void AddWitness(Transaction tx, byte[] vscript, byte[] iscript)
        {
            List<Witness> wit = null;
            if (tx.Witnesses == null)
            {
                wit = new List<Witness>();
            }
            else
            {
                wit = new List<Witness>(tx.Witnesses);
            }
            Witness newwit = new Witness();
            newwit.VerificationScript = vscript;
            newwit.InvocationScript = iscript;
            foreach (var w in wit)
            {
                if (w.ScriptHash == newwit.ScriptHash)
                    throw new Exception("alread have this witness");
            }

            wit.Add(newwit);
            tx.Witnesses = wit.ToArray();
        }

        public static byte[] HexString2Bytes(string str)
        {
            if (str.IndexOf("0x") == 0)
                str = str.Substring(2);
            byte[] outd = new byte[str.Length / 2];
            for (var i = 0; i < str.Length / 2; i++)
            {
                outd[i] = byte.Parse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return outd;
        }

        public static void PushRandomBytes(ScriptBuilder sb, int count = 32)
        {
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            byte[] randomBytes = new byte[count];
            using (System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            BigInteger randomNum = new BigInteger(randomBytes);
            sb.EmitPush(randomNum);
            sb.EmitPush(Neo.VM.OpCode.DROP);
        }

        public static InvocationTransaction MakeTransaction(byte[] script, KeyPair keypair)
        {
            InvocationTransaction tx = new InvocationTransaction
            {
                ChainHash = UInt160.Zero,
                Script = script,
                GasLimit = Fixed8.Zero,
            };

            tx.Attributes = new TransactionAttribute[1];
            tx.Attributes[0] = new TransactionAttribute();
            tx.Attributes[0].Usage = TransactionAttributeUsage.Script;
            tx.Attributes[0].Data = Contract.CreateSignatureRedeemScript(keypair.PublicKey).ToScriptHash().ToArray();

            byte[] data = GetHashData(tx);
            byte[] signdata = Sign(data, keypair.PrivateKey, keypair.PublicKey);
            AddWitness(tx, signdata, keypair.PublicKey);

            return tx;
        }

        public static string GetTransactionRawData(InvocationTransaction tx)
        {
            return tx.ToArray().ToHexString();
        }

        public static async Task<string> SendRawTransaction(string appchainHash, string rawdata)
        {
            MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
            postRawArray.AddArrayValue(appchainHash);
            postRawArray.AddArrayValue(rawdata);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            string result = await Helper.HttpPost(url, postdata);
            return result;
        }

        public static async Task<string> SendRawTransaction(byte[] script, KeyPair keypair, string chainHash)
        {
            InvocationTransaction tx = MakeTransaction(script, keypair);

            string rawdata = tx.ToArray().ToHexString();

            string url;
            byte[] postdata;

            if (Program.ChainID == "Zoro")
            {
                MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
                postRawArray.AddArrayValue(chainHash);
                postRawArray.AddArrayValue(rawdata);

                url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            }
            else
            {
                url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            }

            string result = "";
            try
            {
                result = await Helper.HttpPost(url, postdata);
                
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static async Task<string> InvokeScript(byte[] script, string chainHash)
        {
            string scriptPublish = script.ToHexString();

            byte[] postdata;
            string url;
            if (Program.ChainID == "Zoro")
            {
                MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
                postArray.AddArrayValue(chainHash);
                postArray.AddArrayValue(scriptPublish);

                url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
            }
            else
            {
                url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, new MyJson.JsonNode_ValueString(scriptPublish));
            }

            string result = "";
            try
            {
                result = await Helper.HttpPost(url, postdata);

            }
            catch (Exception)
            {
            }

            return result;
        }
    }
}
