using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace InvokeContractTest
{
    public class Utxo
    {
        public ThinNeo.Hash256 txid;
        public int n;

        public string addr;
        public string asset;
        public decimal value;
        public Utxo(string _addr, ThinNeo.Hash256 _txid, string _asset, decimal _value, int _n)
        {
            this.addr = _addr;
            this.txid = _txid;
            this.asset = _asset;
            this.value = _value;
            this.n = _n;
        }
    }

    class Helper
    {
        public static string MakeRpcUrlPost(string url, string method, out byte[] data, params MyJson.IJsonNode[] _params)
        {
            var json = new MyJson.JsonNode_Object();
            json["id"] = new MyJson.JsonNode_ValueNumber(1);
            json["jsonrpc"] = new MyJson.JsonNode_ValueString("2.0");
            json["method"] = new MyJson.JsonNode_ValueString(method);
            StringBuilder sb = new StringBuilder();
            var array = new MyJson.JsonNode_Array();
            for (var i = 0; i < _params.Length; i++)
            {
                array.Add(_params[i]);
            }
            json["params"] = array;
            data = System.Text.Encoding.UTF8.GetBytes(json.ToString());
            return url;
        }

        public static string MakeRpcUrl(string url, string method, params MyJson.IJsonNode[] _params)
        {
            StringBuilder sb = new StringBuilder();
            if (url.Last() != '/')
            {
                url = url + "/";
            }
            sb.Append(url + "?jsonrpc=2.0&id=1&method=" + method + "&params=[");
            for (var i = 0; i < _params.Length; i++)
            {
                _params[i].ConvertToString(sb);
                if (i != _params.Length - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static async Task<Dictionary<string, List<Utxo>>> GetBalanceByAddress(string api, string _addr)
        {
            MyJson.JsonNode_Object response = (MyJson.JsonNode_Object)MyJson.Parse(await Helper.HttpGet(api + "?method=getutxo&id=1&params=['" + _addr + "']"));
            MyJson.JsonNode_Array resJA = (MyJson.JsonNode_Array)response["result"];
            Dictionary<string, List<Utxo>> _dir = new Dictionary<string, List<Utxo>>();
            foreach (MyJson.JsonNode_Object j in resJA)
            {
                Utxo utxo = new Utxo(j["addr"].ToString(), new ThinNeo.Hash256(j["txid"].ToString()), j["asset"].ToString(), decimal.Parse(j["value"].ToString()), int.Parse(j["n"].ToString()));
                if (_dir.ContainsKey(j["asset"].ToString()))
                {
                    _dir[j["asset"].ToString()].Add(utxo);
                }
                else
                {
                    List<Utxo> l = new List<Utxo>();
                    l.Add(utxo);
                    _dir[j["asset"].ToString()] = l;
                }
            }
            return _dir;
        }

        public static async Task<string> HttpGet(string url)
        {
            WebClient wc = new WebClient();
            return await wc.DownloadStringTaskAsync(url);
        }

        public static async Task<string> HttpPost(string url, byte[] data)
        {
            WebClient wc = new WebClient();
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            byte[] retdata = await wc.UploadDataTaskAsync(url, "POST", data);
            return System.Text.Encoding.UTF8.GetString(retdata);
        }

        public static string GetJsonString(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "ByteArray")
            {
                var bt = ThinNeo.Debug.DebugTool.HexString2Bytes(value.AsString());
                string str = System.Text.Encoding.ASCII.GetString(bt);
                return str;

            }
            return "";
        }

        public static string GetJsonBigInteger(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "ByteArray")
            {
                var bt = ThinNeo.Debug.DebugTool.HexString2Bytes(value.AsString());
                var num = new BigInteger(bt);
                return num.ToString();

            }
            return "";
        }

        public static string GetJsonInteger(MyJson.JsonNode_Object item)
        {
            var type = item["type"].ToString();
            var value = item["value"];
            if (type == "Integer")
            {
                return value.ToString();

            }
            return "";
        }

        public static ThinNeo.Transaction makeTran(Dictionary<string, List<Utxo>> dir_utxos, string targetaddr, ThinNeo.Hash256 assetid, decimal sendcount)
        {
            var tran = new ThinNeo.Transaction();
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            tran.version = 0;
            tran.extdata = null;

            tran.attributes = new ThinNeo.Attribute[0];
            var scraddr = "";
            decimal count = decimal.Zero;
            List<ThinNeo.TransactionInput> list_inputs = new List<ThinNeo.TransactionInput>();
            tran.inputs = list_inputs.ToArray();
            List<ThinNeo.TransactionOutput> list_outputs = new List<ThinNeo.TransactionOutput>();
            tran.outputs = list_outputs.ToArray();

            return tran;
        }
    }
}
