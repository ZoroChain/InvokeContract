﻿using System.Collections.Generic;
using System.IO;
using Zoro;

namespace InvokeContractTest
{
    class Config
    {
        public static MyJson.IJsonNode configJson = null;

        public static Fixed8 GasPrice = Fixed8.One;

        public static decimal GasNEP5Transfer = (decimal)4.5;

        public static Dictionary<string, Fixed8> GasLimit = new Dictionary<string, Fixed8>();

        public static void init(string configPath)
        {
            configJson = MyJson.Parse(File.ReadAllText(configPath));

            GasLimit["NEP5Transfer"] = Fixed8.FromDecimal((decimal)4.5);
            GasLimit["NativeNEP5Transfer"] = Fixed8.FromDecimal(1);
            GasLimit["BCPTransfer"] = Fixed8.FromDecimal(1);
        }

        public static string getValue(string name) {
            return configJson.GetDictItem(name).ToString();
        }

        public static string[] getStringArray(string name)
        {
            MyJson.JsonNode_Array array = configJson.GetDictItem(name).AsList();

            List<string> strList = new List<string>();

            array.ForEach(p => strList.Add(p.AsString()));

            return strList.ToArray();
        }
    }
}
