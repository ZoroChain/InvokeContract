using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using Zoro;

namespace InvokeContractTest
{
    class LocalTest_HashSet : IExample
    {
        public string Name => "LocalTest HashSet 本地测试";

        public string ID => "20";

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        { 
            Console.WriteLine("输入创建Hash的数量（单位：万）:");
            var param = Console.ReadLine();

            int count = int.Parse(param) * 10000;
            if (count <= 0)
                return;

            HashSet<UInt256> hashSet = new HashSet<UInt256>();

            byte[] randomBytes = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            for (int i = 0; i < count; i++)
            {
                rng.GetBytes(randomBytes);
                UInt256 hash = new UInt256(randomBytes);
                hashSet.Add(hash);
            }

            Console.WriteLine("输入进行Hash比较的次数（单位：万）:");

            param = Console.ReadLine();

            count = int.Parse(param) * 10000;
            if (count <= 0)
                return;

            DateTime dt = DateTime.Now;

            Random rand = new Random();

            int match = 0;

            for (int i = 0; i < count; i++)
            {
                rand.NextBytes(randomBytes);

                UInt256 hash = new UInt256(randomBytes);

                if (hashSet.Contains(hash))
                {
                    match++;
                }
            }

            float ratio = ((float)match / count) * 100;

            Console.WriteLine($"总共匹配到了{match}次，占比{ratio}%");

            TimeSpan span = DateTime.Now - dt;
            Console.WriteLine($"耗时:{new DateTime(span.Ticks).TimeOfDay:hh\\:mm\\:ss\\.fff}");
        }
    }
}
