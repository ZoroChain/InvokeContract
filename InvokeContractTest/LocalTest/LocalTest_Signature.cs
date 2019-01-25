using System;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Zoro;
using Zoro.Ledger;
using Zoro.Wallets;
using Zoro.SmartContract;
using Zoro.Persistence;
using Zoro.Persistence.LevelDB;
using Zoro.Network.P2P.Payloads;
using Neo.VM;

namespace InvokeContractTest
{
    class LocalTest_Signature : IExample
    {
        public string Name => "测试Signature的性能";

        private KeyPair keypair;
        private UInt160 scriptHash;
        private UInt160 targetscripthash;

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        {
            string WIF = Config.getValue("WIF");
            string targetWIF = Config.getValue("targetWIF");

            keypair = ZoroHelper.GetKeyPairFromWIF(WIF);
            scriptHash = ZoroHelper.GetPublicKeyHash(keypair.PublicKey);
            targetscripthash = ZoroHelper.GetPublicKeyHashFromWIF(targetWIF);

            string path = Config.getValue("LevelDB");
            Store appStore = new LevelDBStore(path);
            Snapshot snapshot = appStore.GetSnapshot();

            Console.WriteLine("请输入数量:");
            var param = Console.ReadLine();

            int count = int.Parse(param);
            if (count <= 0)
                return;

            Random rnd = new Random();
            DateTime dt = DateTime.Now;
            List<Transaction> txns = new List<Transaction>();

            for (int i = 0; i < count; i++)
            {
                txns.Add(MakeTestTransaction(rnd));
            }

            Console.Write("maketxn, ");
            PrintTimeCost(dt);

            dt = DateTime.Now;
            foreach (var tx in txns)
            {
                tx.Verify(snapshot);
            }

            Console.Write("verify, ");
            PrintTimeCost(dt);

            dt = DateTime.Now;
            foreach (var tx in txns)
            {
                tx.Reverify(snapshot);
            }

            Console.Write("reverify, ");
            PrintTimeCost(dt);

            snapshot.Dispose();
            appStore.Dispose();
        }

        private Transaction MakeTestTransaction(Random rnd)
        {
            Fixed8.TryParse((rnd.Next(1, 10000) * 0.00001).ToString(), out Fixed8 price);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Transfer", Genesis.BcpContractAddress, scriptHash, targetscripthash, new BigInteger(1));

                InvocationTransaction tx = new InvocationTransaction
                {
                    Nonce = Transaction.GetNonce(),
                    Script = sb.ToArray(),
                    GasPrice = price,
                    GasLimit = Fixed8.FromDecimal(1000),
                    Account = ZoroHelper.GetPublicKeyHash(keypair.PublicKey)
                };

                tx.Attributes = new TransactionAttribute[0];

                tx.Witnesses = new Witness[0];
                byte[] data = ZoroHelper.GetHashData(tx);
                byte[] signdata = ZoroHelper.Sign(data, keypair.PrivateKey, keypair.PublicKey);
                ZoroHelper.AddWitness(tx, signdata, keypair.PublicKey);

                return tx;
            }
        }

        private void PrintTimeCost(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            Console.WriteLine($"耗时:{span.TotalMilliseconds}ms");
        }
    }
}
