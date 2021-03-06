﻿using Zoro;
using Zoro.IO;
using Zoro.IO.Json;
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

        public static UInt160 GetPublicKeyHashFromAddress(string address)
        {
            System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            var alldata = Base58.Decode(address);
            var data = alldata.Take(alldata.Length - 4).ToArray();
            var hash = sha256.ComputeHash(data);
            hash = sha256.ComputeHash(hash);
            var hashbts = hash.Take(4).ToArray();
            var datahashbts = alldata.Skip(alldata.Length - 4).ToArray();
            var pkhash = data.Skip(1).ToArray();
            return new UInt160(pkhash);
        }

        public static UInt160 GetMultiSigRedeemScriptHash(int m, KeyPair[] keypairs)
        {
            return Contract.CreateMultiSigRedeemScript(m, keypairs.Select(p => p.PublicKey).ToArray()).ToScriptHash();
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

        public static void AddWitness(Transaction tx, byte[][] signdata, int m, ECPoint[] pubkeys)
        {
            var vscript = Contract.CreateMultiSigRedeemScript(m, pubkeys).ToArray();

            using (var sb = new ScriptBuilder())
            {
                int i = 0;
                foreach (var sig in signdata)
                {
                    sb.EmitPush(sig);
                    if (++i >= m)
                        break;
                }

                var iscript = sb.ToArray();

                AddWitness(tx, vscript, iscript);
            }
        }

        public static void AddWitness(Transaction tx, byte[] signdata, ECPoint pubkey)
        {
            var vscript = Contract.CreateSignatureRedeemScript(pubkey).ToArray();

            using (var sb = new ScriptBuilder())
            {
                sb.EmitPush(signdata);

                var iscript = sb.ToArray();

                AddWitness(tx, vscript, iscript);
            }
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

        public static async Task<string> InvokeScript(byte[] script, string chainHash)
        {
            string scriptPublish = script.ToHexString();

            byte[] postdata;
            string url;
            MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
            postArray.AddArrayValue(chainHash);
            postArray.AddArrayValue(scriptPublish);

            url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());

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

        public static string GetJsonValue(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "ByteArray")
            {
                var bt = HexString2Bytes(value.AsString());
                var num = new BigInteger(bt);
                return num.ToString();

            }
            else if (type == "Integer")
            {
                return value.ToString();

            }
            return "";
        }

        public static InvocationTransaction MakeTransaction(byte[] script, KeyPair keypair, Fixed8 gasLimit, Fixed8 gasPrice)
        {
            InvocationTransaction tx = new InvocationTransaction
            {
                Nonce = Transaction.GetNonce(),
                Script = script,
                GasPrice = gasPrice,
                GasLimit = gasLimit.Ceiling(),
                Account = GetPublicKeyHash(keypair.PublicKey)
            };

            tx.Attributes = new TransactionAttribute[0];

            byte[] data = GetHashData(tx);
            byte[] signdata = Sign(data, keypair.PrivateKey, keypair.PublicKey);
            AddWitness(tx, signdata, keypair.PublicKey);

            return tx;
        }

        public static InvocationTransaction MakeMultiSignatureTransaction(byte[] script, int m, KeyPair[] keypairs, Fixed8 gasLimit, Fixed8 gasPrice)
        {
            InvocationTransaction tx = new InvocationTransaction
            {
                Nonce = Transaction.GetNonce(),
                Script = script,
                GasPrice = gasPrice,
                GasLimit = gasLimit.Ceiling(),
                Account = GetMultiSigRedeemScriptHash(m, keypairs)
            };

            int count = keypairs.Length;
            ECPoint[] pubkeys = keypairs.Select(p => p.PublicKey).ToArray();

            tx.Attributes = new TransactionAttribute[0];

            byte[] data = GetHashData(tx);
            byte[][] signatures = new byte[count][];

            int i = 0;
            foreach (KeyPair keypair in keypairs.OrderBy(p => p.PublicKey))
            {
                signatures[i++] = Sign(data, keypair.PrivateKey, keypair.PublicKey);
            }

            AddWitness(tx, signatures, m, pubkeys);
            return tx;
        }

        public static async Task<string> PostHttpRequest(string method, string rawdata, string chainHash)
        {
            string url;
            byte[] postdata;

            MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
            postRawArray.AddArrayValue(chainHash);
            postRawArray.AddArrayValue(rawdata);

            url = Helper.MakeRpcUrlPost(Program.local, method, out postdata, postRawArray.ToArray());

            string result = "";
            try
            {
                result = await Helper.HttpPost(url, postdata);

            }
            catch (Exception)
            {
                //Console.WriteLine(e.Message);
            }

            return result;
        }

        public static async Task<string> SendRawTransaction(Transaction tx, string chainHash)
        {
            return await PostHttpRequest("sendrawtransaction", tx.ToArray().ToHexString(), chainHash);
        }

        public static async Task<decimal> EstimateGas(Transaction tx, string chainHash)
        {
            var response = await PostHttpRequest("estimategas", tx.ToArray().ToHexString(), chainHash);
            JObject json_response = JObject.Parse(response);
            JObject json_result = json_response["result"];
            JObject json_gas_consumed = json_result["gas_consumed"];
            decimal gas_consumed = (decimal) json_gas_consumed.AsNumber();
            return gas_consumed;
        }

        public static async Task<string> SendInvocationTransaction(byte[] script, int m, KeyPair[] keypairs, string chainHash, Fixed8 gasPrice)
        {
            InvocationTransaction tx = MakeMultiSignatureTransaction(script, m, keypairs, Fixed8.Zero, gasPrice);

            decimal gas_consumed = await EstimateGas(tx, chainHash);

            tx = MakeMultiSignatureTransaction(script, m, keypairs, Fixed8.FromDecimal(gas_consumed), gasPrice);

            return await SendRawTransaction(tx, chainHash);
        }

        public static async Task<string> SendInvocationTransaction(byte[] script, KeyPair keypair, string chainHash, Fixed8 gasPrice)
        {
            InvocationTransaction tx = MakeTransaction(script, keypair, Fixed8.Zero, gasPrice);

            decimal gas_consumed = await EstimateGas(tx, chainHash);

            tx = MakeTransaction(script, keypair, Fixed8.FromDecimal(gas_consumed), gasPrice);

            return await SendRawTransaction(tx, chainHash);
        }


        public static async Task<string> SendInvocationTransaction(byte[] script, KeyPair keypair, string chainHash, Fixed8 gasLimit, Fixed8 gasPrice)
        {
            InvocationTransaction tx = MakeTransaction(script, keypair, gasLimit, gasPrice);

            return await SendRawTransaction(tx, chainHash);
        }
    }
}
