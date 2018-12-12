using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class RetrieveNativeNEP5 : IExample
    {
        public string Name => "RetrieveNativeNEP5 进行一次native nep5货币分配";

        public string ID => "13";

        public async Task StartAsync()
        {
            Console.WriteLine("转账金额");
            string transferValue = Console.ReadLine();

            string chainHash = Config.getValue("ChainHash");
            string targetWIF = Config.getValue("targetWIF");
            string nativeNEP5 = Config.getValue("NativeNEP5");
            string[] wif_list = Config.getStringArray("NativeNEP5Issuer");
            UInt256 assetId = UInt256.Parse(nativeNEP5);

            byte decimals = await GetDecimals(assetId, chainHash);

            BigInteger value = BigInteger.Parse(transferValue) * BigInteger.Pow(10, decimals);

            await SendTransaction(assetId, wif_list, targetWIF, BigInteger.Parse(value.ToString()), chainHash);
        }

        public async Task SendTransaction(UInt256 nativeNEP5AssetId, string[] wif_list, string targetWIF, BigInteger value, string chainHash)
        {
            KeyPair[] keypairs = wif_list.Select(p => ZoroHelper.GetKeyPairFromWIF(p)).ToArray();
            int m = keypairs.Length / 2 + 1;

            UInt160 scriptHash = ZoroHelper.GetMultiSigRedeemScriptHash(m, keypairs);

            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.NativeNEP5.Transfer", nativeNEP5AssetId, scriptHash, targetscripthash, value);

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendRawTransaction(sb.ToArray(), m, keypairs, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        decimal GetGasConsumed(string info)
        {
            MyJson.JsonNode_Object json_result_array = MyJson.Parse(info) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;

            var consume = json_result_obj["gas_consumed"].ToString();
            return decimal.Parse(consume);
        }

        public async Task<byte> GetDecimals(UInt256 nativeNEP5AssetId, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Decimals", nativeNEP5AssetId);

                var result = await ZoroHelper.InvokeScript(sb.ToArray(), chainHash);

                return GetDecimals(result);
            }
        }

        byte GetDecimals(string result)
        {
            byte decimals = 0;

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;

            if (json_result_array.ContainsKey("result"))
            {
                MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
                MyJson.JsonNode_Array stack = json_result_obj["stack"] as MyJson.JsonNode_Array;

                if (stack != null && stack.Count >= 1)
                {
                    string value = ZoroHelper.GetJsonValue(stack[0] as MyJson.JsonNode_Object);
                    decimals = byte.Parse(value);
                }
            }
            return decimals;
        }
    }
}
