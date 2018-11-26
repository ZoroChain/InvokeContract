using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Neo.VM;
using ThinNeo;
using ScriptBuilder = ThinNeo.ScriptBuilder;

namespace InvokeContractTest
{
    public class CrossChainTransaction : IExample
    {
        public string Name => "transfer 进行一次 Neo 和 Zoro 间的跨链交易";

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
            Console.WriteLine("输入兑换方向，1：NEO --> Zoro; 2: Zoro --> Neo;   3：查询目标账户余额");
            var param1 = Console.ReadLine();
            ChainHash = Config.getValue("ChainHash");
            WIF = Config.getValue("CrossAccount");

            if (int.Parse(param1) == 1)
            {
                Console.WriteLine("转账金额：");
                var param3 = Console.ReadLine();
                transferValue = Math.Round(decimal.Parse(param3) * 100000000, 0).ToString();
                direction = 1;
                targetAddress = Config.getValue("NeoBank");
                ContractHash = Config.getValue("NeoBcp");
                var api = Config.getValue("NeoRpcUrl");
                await NeoTransferAsync(api, targetAddress, ContractHash, transferValue); 
            }

            if (int.Parse(param1) == 2)
            {
                Console.WriteLine("转账金额：");
                var param3 = Console.ReadLine();
                transferValue = Math.Round(decimal.Parse(param3) * 100000000, 0).ToString();
                direction = 2;
                targetAddress = Config.getValue("ZoroBank");
                ContractHash = Config.getValue("ZoroBcp");
                await TransferNEP5Async(ChainHash, targetAddress, ContractHash, transferValue);
            }

            if (int.Parse(param1) == 3)
            {
                var api = Config.getValue("NeoRpcUrl");
                byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
                byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
                string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
                var scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
                Console.WriteLine($"Address: {address}");
                await GetZoroBalanceOfAsync(ChainHash, address, Config.getValue("ZoroBcp"));
                await GetNeoBalanceOfAsync(api, address, Config.getValue("NeoBcp"));
            }
        }

        private async Task GetNeoBalanceOfAsync(string api, string address, string contractHash)
        {
            byte[] data = null;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(addr)" + address);
                sb.EmitParamJson(array);
                sb.EmitPushString("balanceOf");
                sb.EmitAppCall(new Hash160(contractHash));//合约脚本hash
                data = sb.ToArray();
            }

            string script = ThinNeo.Helper.Bytes2HexString(data);
            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(api, "invokescript", out postdata, new MyJson.JsonNode_ValueString(script));
            var result = await Helper.HttpPost(url, postdata);
            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
            MyJson.JsonNode_Array stack = json_result_obj["stack"] as MyJson.JsonNode_Array;
            decimal value = decimal.Parse(Helper.GetJsonBigInteger(stack[0] as MyJson.JsonNode_Object)) / (decimal)100000000.00000000;
            Console.WriteLine($"Neo balance : {value}");
        }

        private async Task GetZoroBalanceOfAsync(string chainHash, string address, string contractHash)
        {
            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(addr)" + address);
            sb.EmitParamJson(array);
            sb.EmitPushString("balanceOf");
            sb.EmitAppCall(new Hash160(contractHash));

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());
            string url;
            byte[] postdata;

            MyJson.JsonNode_Array postArray = new MyJson.JsonNode_Array();
            postArray.AddArrayValue(chainHash);
            postArray.AddArrayValue(scriptPublish);

            url = Helper.MakeRpcUrlPost(Program.local, "invokescript", out postdata, postArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);

            MyJson.JsonNode_Object json_result_array = MyJson.Parse(result) as MyJson.JsonNode_Object;
            MyJson.JsonNode_Object json_result_obj = json_result_array["result"] as MyJson.JsonNode_Object;
            MyJson.JsonNode_Array stack = json_result_obj["stack"] as MyJson.JsonNode_Array;
            decimal value = decimal.Parse(Helper.GetJsonBigInteger(stack[0] as MyJson.JsonNode_Object)) / (decimal)100000000.00000000;
            Console.WriteLine($"Zoro balance : {value}");
        }

        public async Task NeoTransferAsync(string api, string targetAddress, string contractHash, string transferValue)
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            var address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            byte[] script = null;
            using (var sb = new ThinNeo.ScriptBuilder())
            {
                var array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(addr)" + address);
                array.AddArrayValue("(hex160)" + targetAddress);//NeoBank address   AMjCDmrbfcBxGPitHcdrUYRyPXD7DfC52c
                array.AddArrayValue("(int)" + transferValue);
                byte[] randomBytes = new byte[32];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                BigInteger randomNum = new BigInteger(randomBytes);
                sb.EmitPushNumber(randomNum);
                sb.Emit(ThinNeo.VM.OpCode.DROP);
                sb.EmitParamJson(array);//参数倒序入
                sb.EmitPushString("transfer");//参数倒序入
                sb.EmitAppCall(new Hash160(contractHash));//nep5脚本
                script = sb.ToArray();
            }

            ThinNeo.Transaction tran = new Transaction();
            tran.inputs = new ThinNeo.TransactionInput[0];
            tran.outputs = new TransactionOutput[0];
            tran.attributes = new ThinNeo.Attribute[1];
            tran.attributes[0] = new ThinNeo.Attribute();
            tran.attributes[0].usage = TransactionAttributeUsage.Script;
            tran.attributes[0].data = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
            tran.version = 1;
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            var idata = new ThinNeo.InvokeTransData();

            tran.extdata = idata;
            idata.script = script;
            idata.gas = 0;

            byte[] msg = tran.GetMessage();
            string msgstr = ThinNeo.Helper.Bytes2HexString(msg);
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            byte[] postdata;
            var url = Helper.MakeRpcUrlPost(api, "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            var result = await Helper.HttpPost(url, postdata);
            Console.WriteLine(result + " txid: " + txid);
        }

        public async Task TransferNEP5Async(string ChainHash, string targetAddress, string ContractHash, string transferValue)
        {
            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(WIF);
            var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            var address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);
            ScriptBuilder sb = new ScriptBuilder();
            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(addr)" + address);
            array.AddArrayValue("(hex160)" + targetAddress); //ZoroBank address   AUB7tMoKTzN33iVVqhz98vnT3KiG4bqx3f
            array.AddArrayValue("(int)" + transferValue);
            byte[] randomBytes = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            BigInteger randomNum = new BigInteger(randomBytes);
            sb.EmitPushNumber(randomNum);
            sb.Emit(ThinNeo.VM.OpCode.DROP);
            sb.EmitParamJson(array);
            sb.EmitPushString("transfer");
            sb.EmitAppCall(new Hash160(ContractHash));
            Hash160 scripthash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
            
            byte[] postdata;
            ThinNeo.InvokeTransData extdata = new ThinNeo.InvokeTransData();
            extdata.script = sb.ToArray();
            extdata.gas = 0;
            ThinNeo.Transaction tran = new Transaction();
            tran.inputs = new ThinNeo.TransactionInput[0];
            tran.outputs = new TransactionOutput[0];
            tran.attributes = new ThinNeo.Attribute[1];
            tran.attributes[0] = new ThinNeo.Attribute();
            tran.version = 1;
            tran.extdata = extdata;
            tran.type = ThinNeo.TransactionType.InvocationTransaction;

            //附加鉴证
            tran.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
            tran.attributes[0].data = scripthash;

            byte[] msg = tran.GetMessage();
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            MyJson.JsonNode_Array postRawArray = new MyJson.JsonNode_Array();
            postRawArray.AddArrayValue(chainHash); //根链
            postRawArray.AddArrayValue(rawdata);

            var url = Helper.MakeRpcUrlPost(Program.local, "sendrawtransaction", out postdata, postRawArray.ToArray());
            var result = await Helper.HttpPost(url, postdata);
            Console.WriteLine(result + " txid: " + txid);
        }
    }
}
