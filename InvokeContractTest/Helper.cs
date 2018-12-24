using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

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

        public static Dictionary<string, List<Utxo>> GetBalanceByAddress(string api, string _addr, ref Dictionary<string, string> usedUtxoDic)
        {
            MyJson.JsonNode_Object response = (MyJson.JsonNode_Object)MyJson.Parse(Helper.HttpGet(api + "?method=getutxo&id=1&params=['" + _addr + "']").Result);
            MyJson.JsonNode_Array resJA = (MyJson.JsonNode_Array)response["result"];
            Dictionary<string, List<Utxo>> _dir = new Dictionary<string, List<Utxo>>();
            List<string> usedList = new List<string>(usedUtxoDic.Keys);
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

                for (int i = usedList.Count - 1; i >= 0; i--)
                {
                    if (usedUtxoDic[usedList[i]] == utxo.txid.ToString())
                    {
                        usedUtxoDic.Remove(usedList[i]);
                        usedList.Remove(usedList[i]);
                    }
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
            else if (type == "Integer")
            {
                return value.ToString();
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
            List<ThinNeo.TransactionInput> list_inputs = new List<ThinNeo.TransactionInput>();
            tran.inputs = list_inputs.ToArray();
            List<ThinNeo.TransactionOutput> list_outputs = new List<ThinNeo.TransactionOutput>();
            tran.outputs = list_outputs.ToArray();

            return tran;
        }

        public static ThinNeo.Transaction makeGasTran(ref List<Utxo> list_Gas, Dictionary<string, string> usedUtxoDic, Hash256 assetid, decimal gasfee)
        {
            var tran = new ThinNeo.Transaction();
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            tran.version = 0;//0 or 1

            tran.attributes = new ThinNeo.Attribute[0];
            var scraddr = "";

            decimal count = decimal.Zero;
            List<ThinNeo.TransactionInput> list_inputs = new List<ThinNeo.TransactionInput>();
            for (var i = list_Gas.Count - 1; i >= 0; i--)
            {
                if (usedUtxoDic.ContainsKey(list_Gas[i].txid.ToString() + list_Gas[i].n))
                    continue;

                ThinNeo.TransactionInput input = new ThinNeo.TransactionInput();
                input.hash = list_Gas[i].txid;
                input.index = (ushort)list_Gas[i].n;
                list_inputs.Add(input);
                count += list_Gas[i].value;
                scraddr = list_Gas[i].addr;
                list_Gas.Remove(list_Gas[i]);
                if (count >= gasfee)
                    break;
            }

            tran.inputs = list_inputs.ToArray();
            if (count >= gasfee)//输入大于等于输出
            {
                List<ThinNeo.TransactionOutput> list_outputs = new List<ThinNeo.TransactionOutput>();
                //输出
                //if (gasfee > decimal.Zero && targetaddr != null)
                //{
                //    ThinNeo.TransactionOutput output = new ThinNeo.TransactionOutput();
                //    output.assetId = assetid;
                //    output.value = gasfee;
                //    output.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetaddr);
                //    list_outputs.Add(output);
                //}

                //找零
                var change = count - gasfee;
                if (change > decimal.Zero)
                {
                    decimal splitvalue = (decimal)0.01;
                    int i = 0;
                    while (change > splitvalue && list_Gas.Count - 10 < usedUtxoDic.Count)
                    {
                        ThinNeo.TransactionOutput outputchange = new ThinNeo.TransactionOutput();
                        outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(scraddr);
                        outputchange.value = splitvalue;
                        outputchange.assetId = assetid;
                        list_outputs.Add(outputchange);
                        change -= splitvalue;
                        i += 1;
                        if (i > 50)
                        {
                            break;
                        }
                    }

                    if (change > 0)
                    {
                        ThinNeo.TransactionOutput outputchange = new ThinNeo.TransactionOutput();
                        outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(scraddr);
                        outputchange.value = change;
                        outputchange.assetId = assetid;
                        list_outputs.Add(outputchange);
                    }

                }

                tran.outputs = list_outputs.ToArray();
            }
            else
            {
                throw new Exception("no enough money.");
            }
            return tran;
        }
    }
}
