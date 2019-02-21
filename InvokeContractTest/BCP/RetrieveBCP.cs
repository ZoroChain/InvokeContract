using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Globalization;
using Zoro;
using Zoro.Ledger;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class RetrieveBCP : IExample
    {
        public string Name => "RetrieveToken 向发行账户申请代币";
        
        public async Task StartAsync()
        {
            string chainHash = Config.getValue("ChainHash");
            string targetWIF = Config.getValue("WIF");
            string[] wif_list = Config.getStringArray("BCPIssuer");
            Console.Write("Choose Token Type，0 - BCP, 1 - BCT");
            int transType = int.Parse(Console.ReadLine());

            Console.WriteLine("转账金额");
            string transferValue = Console.ReadLine();

            if (transType == 0)
            {
                UInt160 assetId = Genesis.BcpContractAddress;

                byte decimals = await GetDecimals(assetId, chainHash);
                Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));
                await SendTransaction(assetId, wif_list, targetWIF, new BigInteger(value), chainHash);
            }
            if (transType == 1)
            {
                UInt160 assetId = Genesis.BctContractAddress;

                byte decimals = await GetDecimals(assetId, chainHash);
                Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));
                await SendMintTokenTransaction(assetId, wif_list, targetWIF, new BigInteger(value), chainHash);
            }
        }

        async Task SendMintTokenTransaction(UInt160 assetId, string[] wif_list, string targetWIF, BigInteger value, string chainHash)
        {
            KeyPair[] keypairs = wif_list.Select(p => ZoroHelper.GetKeyPairFromWIF(p)).ToArray();
            int m = keypairs.Length - (keypairs.Length - 1) / 3;

            UInt160 scriptHash = ZoroHelper.GetMultiSigRedeemScriptHash(m, keypairs);

            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "MintToken", assetId, targetscripthash, value);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), m, keypairs, chainHash, Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        async Task SendTransaction(UInt160 assetId, string[] wif_list, string targetWIF, BigInteger value, string chainHash)
        {
            KeyPair[] keypairs = wif_list.Select(p => ZoroHelper.GetKeyPairFromWIF(p)).ToArray();
            int m = keypairs.Length - (keypairs.Length - 1) / 3;

            UInt160 scriptHash = ZoroHelper.GetMultiSigRedeemScriptHash(m, keypairs);

            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Transfer", assetId, scriptHash, targetscripthash, value);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), m, keypairs, chainHash, Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        async Task<byte> GetDecimals(UInt160 assetId, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Decimals", assetId);

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
    }
}
