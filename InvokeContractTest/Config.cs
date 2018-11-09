﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InvokeContractTest
{
    class Config
    {
        public static MyJson.IJsonNode configJson = null;

        public static void init(string configPath) {
            configJson = MyJson.Parse(File.ReadAllText(configPath));
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
