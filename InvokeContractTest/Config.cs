using System;
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
    }
}
