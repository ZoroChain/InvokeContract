using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Zoro;
using Zoro.Ledger;
using Zoro.Wallets;
using Zoro.Network.P2P.Payloads;
using Neo.VM;

namespace InvokeContractTest
{
    class LocalTest_MemPool : IExample
    {
        public string Name => "测试MemPool的性能";

        private MemoryPool mempool = new MemoryPool(50_000);

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

            while (true)
            {
                Console.WriteLine("请选择的测试类型:");
                Console.WriteLine("1.AddVerified");
                Console.WriteLine("2.MoveToUnverified");
                Console.WriteLine("3.Reverify");
                Console.WriteLine("4.退出");

                int cmd = int.Parse(Console.ReadLine());
                if (cmd == 1)
                {
                    AddVerifed();
                }
                else if (cmd == 2)
                {
                    MoveToUnverified();
                }
                else if (cmd == 3)
                {
                    Reverify();
                }
                else if (cmd == 4)
                {
                    mempool.Clear();
                    break;
                }
            }
        }

        private void AddVerifed()
        {
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

            Console.Write("MakeTxn, count:{0}, ", count);
            PrintTimeCost(dt);

            dt = DateTime.Now;

            int succ = 0;
            for (int i = 0;i <count;i ++)
            {
                if (mempool.TryAddVerified(txns[i]))
                    succ++;
            }

            Console.Write("succ:{0}, total:{1}, ", succ, mempool.Count);
            PrintTimeCost(dt);
        }

        private void MoveToUnverified()
        {
            DateTime dt = DateTime.Now;

            mempool.ResetToUnverified();

            Console.Write("ResetToUnverified:{0}, ", mempool.Count);
            PrintTimeCost(dt);
        }

        private void Reverify()
        {
            Console.WriteLine("请输入每次搬移的数量:");
            var param = Console.ReadLine();

            int step = int.Parse(param);
            if (step <= 0)
                return;

            DateTime dt = DateTime.Now;

            int count = mempool.UnverifiedCount;
            while (mempool.HasUnverified)
            {
                Transaction[] unverfied = mempool.TakeUnverifiedTransactions(step);

                foreach (var tx in unverfied)
                {
                    mempool.SetVerifyState(tx.Hash, true);
                }
            }

            TimeSpan span = DateTime.Now - dt;
            Console.WriteLine($"耗时:{span.TotalMilliseconds}ms, 平均耗时:{span.TotalMilliseconds/(count/step)}ms");
        }

        private Transaction MakeTestTransaction(Random rnd)
        {
            Fixed8.TryParse((rnd.Next(1, 10000) * 0.0001).ToString(), out Fixed8 price);

            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall("Zoro.NativeNEP5.Call", "Transfer", Genesis.BcpContractAddress, scriptHash, targetscripthash, new BigInteger(1));

                InvocationTransaction tx = new InvocationTransaction
                {
                    Nonce = Transaction.GetNonce(),
                    Script = sb.ToArray(),
                    GasPrice = price,
                    GasLimit = Fixed8.FromDecimal((decimal)5),
                    Account = ZoroHelper.GetPublicKeyHash(keypair.PublicKey)
                };

                tx.Attributes = new TransactionAttribute[0];
                tx.Witnesses = new Witness[0];
                //byte[] data = ZoroHelper.GetHashData(tx);
                //byte[] signdata = ZoroHelper.Sign(data, keypair.PrivateKey, keypair.PublicKey);
                //ZoroHelper.AddWitness(tx, signdata, keypair.PublicKey);

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
