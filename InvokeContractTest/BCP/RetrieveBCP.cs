﻿using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Globalization;
using Zoro;
using Zoro.Wallets;
using Neo.VM;

namespace InvokeContractTest
{
    class RetrieveBCP : IExample
    {
        public string Name => "RetrieveBCP 向发行账户申请BCP货币";
        
        public async Task StartAsync()
        {
            Console.WriteLine("转账金额");
            string transferValue = Console.ReadLine();

            string chainHash = Config.getValue("ChainHash");
            string targetWIF = Config.getValue("WIF");
            string BCPHash = Config.getValue("BCPHash");
            string[] wif_list = Config.getStringArray("BCPIssuer");
            UInt256 assetId = UInt256.Parse(BCPHash);

            byte decimals = await GetDecimals(assetId, chainHash);

            Decimal value = Decimal.Parse(transferValue, NumberStyles.Float) * new Decimal(Math.Pow(10, decimals));

            await SendTransaction(assetId, wif_list, targetWIF, new BigInteger(value), chainHash);
        }

        async Task SendTransaction(UInt256 assetId, string[] wif_list, string targetWIF, BigInteger value, string chainHash)
        {
            KeyPair[] keypairs = wif_list.Select(p => ZoroHelper.GetKeyPairFromWIF(p)).ToArray();
            int m = keypairs.Length / 2 + 1;

            UInt160 scriptHash = ZoroHelper.GetMultiSigRedeemScriptHash(m, keypairs);

            UInt160 targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                ZoroHelper.PushRandomBytes(sb);

                sb.EmitSysCall("Zoro.GlobalAsset.Transfer", assetId, scriptHash, targetscripthash, value);

                decimal gas = await ZoroHelper.GetScriptGasConsumed(sb.ToArray(), chainHash);

                var result = await ZoroHelper.SendInvocationTransaction(sb.ToArray(), m, keypairs, chainHash, Fixed8.FromDecimal(gas), Config.GasPrice);

                MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
                Console.WriteLine(resJO.ToString());
            }
        }

        async Task<byte> GetDecimals(UInt256 assetId, string chainHash)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.GlobalAsset.GetPrecision", assetId);

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