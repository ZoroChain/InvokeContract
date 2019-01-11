using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Zoro;

namespace InvokeContractTest
{
    class LocalTest_ConcurrentDictionary : IExample
    {
        public string Name => "ConcurrentDictionary 性能测试";

        private ConcurrentDictionary<UInt256, byte[]> dict = new ConcurrentDictionary<UInt256, byte[]>();

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        {
            while (true)
            {
                Console.WriteLine("请选择的测试类型:");
                Console.WriteLine("1.插入");
                Console.WriteLine("2.查询");
                Console.WriteLine("3.删除");
                Console.WriteLine("4.结束");

                int cmd = int.Parse(Console.ReadLine());
                if (cmd == 1)
                {
                    InsertTest();
                }
                else if (cmd == 2)
                {
                    QueryTest();
                }
                else if (cmd == 3)
                {
                    DeleteTest();
                }
                else if (cmd == 4)
                {
                    break;
                }
            }
        }        

        private void InsertTest()
        {
            Console.WriteLine("输入创建的数量（单位：万）:");
            var param = Console.ReadLine();

            int count = int.Parse(param) * 10000;
            if (count <= 0)
                return;

            Random rand = new Random();
            int size = rand.Next(150, 300);

            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            DateTime dt = DateTime.Now;

            for (int i = 0; i < count; i++)
            {
                byte[] hashBytes = new byte[32];
                rng.GetBytes(hashBytes);

                byte[] randomBytes = new byte[size];
                rng.GetBytes(randomBytes);

                UInt256 hash = new UInt256(hashBytes);
                dict.TryAdd(hash, randomBytes);
            }

            PrintTimeCost(dt);
        }

        private void QueryTest()
        {
            if (dict.Count == 0)
            {
                Console.WriteLine("请先插入数据.");
                return;
            }

            Console.WriteLine("输入进行查询的次数（单位：千）:");

            int count = int.Parse(Console.ReadLine()) * 1000;
            if (count <= 0)
                return;

            DateTime dt = DateTime.Now;

            Random rand = new Random();

            int match = 0;

            for (int i = 0; i < count; i++)
            {
                byte[] hashBytes = new byte[32];
                rand.NextBytes(hashBytes);

                UInt256 hash = new UInt256(hashBytes);

                if (dict.ContainsKey(hash))
                {
                    match++;
                }
            }

            float ratio = ((float)match / count) * 100;

            Console.WriteLine($"总共有{dict.Count / 10000}万条数据，进行了{count/1000}千次查询，匹配到了{match}次，占比{ratio}%");
            PrintTimeCost(dt);
        }

        private void DeleteTest()
        {
            if (dict.Count == 0)
            {
                Console.WriteLine("请先插入数据.");
                return;
            }

            Console.WriteLine("输入要删除的数据条数（单位：千）:");

            int count = int.Parse(Console.ReadLine()) * 1000;
            if (count <= 0)
                return;

            if (count > dict.Count)
                count = dict.Count;

            UInt256[] hashes = dict.Keys.Take(count).ToArray();

            DateTime dt = DateTime.Now;

            foreach (var hash in hashes)
            {
                dict.TryRemove(hash, out byte[] _);
            }

            Console.WriteLine($"总共删除了{count/1000}千条数据.");
            PrintTimeCost(dt);
        }

        private void PrintTimeCost(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            Console.WriteLine($"耗时:{new DateTime(span.Ticks).TimeOfDay:hh\\:mm\\:ss\\.fff}");
        }
    }
}
