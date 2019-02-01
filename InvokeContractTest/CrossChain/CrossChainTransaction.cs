using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Neo.VM;
using ThinNeo;
using Zoro;
using Zoro.Wallets;
using ScriptBuilder = Neo.VM.ScriptBuilder;

namespace InvokeContractTest
{
    public class CrossChainTransaction : IExample
    {
        public string Name => "transfer 进行一次 Zoro 和 Neo 间的跨链交易";

        public string ID => "11";

        private string chainHash;
        private string wif;
        private string targetwif;
        private string contractHash;

        public string ChainHash
        {
            get => chainHash;
            set => chainHash = value;
        }

        public string WIF
        {
            get => wif;
            set => wif = value;
        }

        public string targetAddress
        {
            get => targetwif;
            set => targetwif = value;
        }

        public string ContractHash
        {
            get => contractHash;
            set => contractHash = value;
        }

        public string transferValue;
        public int direction = 1;

        public async Task StartAsync()
        {
            Console.WriteLine("0: Zoro --> Neo 转账;   1：查询目标账户余额");
            var param1 = Console.ReadLine();
            ChainHash = Config.getValue("ChainHash");
            WIF = Config.getValue("CrossAccount");
            string nativeNEP5AssetId = Config.getValue("NativeNEP5");
            string[] ChainHashList = Config.getStringArray("ChainHashList");
            string zoroBank = Config.getValue("ZoroBank");
            
            if (int.Parse(param1) == 0)
            {
                Console.WriteLine($"AssetId:{nativeNEP5AssetId}");
                byte decimals = await GetNativeNEP5Decimals(nativeNEP5AssetId, chainHash);
                Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

                await TransferNativeNEP5(chainHash, WIF, zoroBank, nativeNEP5AssetId, new BigInteger(value));
            }

            if (int.Parse(param1) == 1)
            {
                KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
                UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
                UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromAddress(targetAddress);

                await BalanceOfNativeNEP5(UInt160.Parse(nativeNEP5AssetId), targetscripthash, ChainHashList);
            }
        }

        async Task BalanceOfNativeNEP5(UInt160 nativeNEP5AssetId, UInt160 address, string[] chainHashList)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.BalanceOf", nativeNEP5AssetId, address);
                sb.EmitSysCall("Zoro.NativeNEP5.Decimals", nativeNEP5AssetId);

                Console.WriteLine($"NativeNEP5: {nativeNEP5AssetId}");
                foreach (var chainHash in chainHashList)
                {
                    var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);
                    var value = GetBalanceFromJson(info);
                    string chainName = chainHash.Length > 0 ? chainHash : "Root";
                    Console.WriteLine($"balanceOf: {value}, chain:{chainName}");
                }
            }
        }
        
        async Task<byte> GetNativeNEP5Decimals(string assetId, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Decimals", UInt160.Parse(assetId));

                var info = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);

                return ParseDecimals(info);
            }
        }

        byte ParseDecimals(string info)
        {
            byte decimals = 0;

            MyJson.JsonNode_Object json = MyJson.Parse(info) as MyJson.JsonNode_Object;

            if (json.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result = json["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array json_stack = json_result["stack"] as MyJson.JsonNode_Array;

                if (json_stack != null && json_stack.Count >= 1)
                {
                    string value = ZoroHelper.GetJsonValue(json_stack[0] as MyJson.JsonNode_Object);
                    decimals = byte.Parse(value);
                }
            }
            return decimals;
        }

        public async Task TransferNativeNEP5(string chainHash, string WIF, string targetscripthash, string assetId, BigInteger value)
        {
            KeyPair keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            UInt160 scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Transfer", UInt160.Parse(assetId), scriptHash, targetscripthash, value);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), keypair, chainHash, Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        string GetBalanceFromJson(string info)
        {
            string result = "";
            MyJson.JsonNode_Object json = MyJson.Parse(info) as MyJson.JsonNode_Object;

            if (json.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result = json["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result["stack"] as MyJson.JsonNode_Array;

                if (stack != null && stack.Count >= 2)
                {
                    string balance = ZoroHelper.GetJsonValue(stack[0] as MyJson.JsonNode_Object);
                    string decimals = ZoroHelper.GetJsonValue(stack[1] as MyJson.JsonNode_Object);

                    Decimal value = Decimal.Parse(balance) / new Decimal(Math.Pow(10, int.Parse(decimals)));
                    string fmt = "{0:N" + decimals + "}";
                    result = string.Format(fmt, value);
                }
            }
            else if (json.ContainsKey("error"))
            {
                MyJson.JsonNode_Object json_error_obj = json["error"] as MyJson.JsonNode_Object;
                result = json_error_obj.ToString();
            }

            return result;
        }

    }
}
