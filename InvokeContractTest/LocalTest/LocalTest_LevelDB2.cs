using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Zoro.IO.Data.LevelDB;

namespace InvokeContractTest
{
    class LocalTest_LevelDB2 : IExample
    {
        public string Name => "测试LevelDB的性能";

        private string path = "../../../../../leveldb_test";

        private RandomNumberGenerator rng = RandomNumberGenerator.Create();
        private Random rand = new Random();

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        {
            int MB = 1024 * 1024;
            int writeBufferSize = 2 * MB;
            int insertCount = 1000;
            int queryCount = 1000;
            int runTimes = 100;
            int iterationCount = 10;

            int inputValue = 0;

            Console.WriteLine("请输入WriteBuffer的大小:[2MB]");
            if (int.TryParse(Console.ReadLine(), out inputValue) && inputValue > 0)
                writeBufferSize = inputValue * MB;

            Console.WriteLine("请输入Insert的次数:[1000]");
            if (int.TryParse(Console.ReadLine(), out inputValue))
                insertCount = inputValue;

            Console.WriteLine("请输入Query的次数:[1000]");
            if (int.TryParse(Console.ReadLine(), out inputValue))
                queryCount = inputValue;

            Console.WriteLine("请输入每个迭代的运行次数:[100]");
            if (int.TryParse(Console.ReadLine(), out inputValue))
                runTimes = inputValue;

            Console.WriteLine("请输入迭代次数:[10]");
            if (int.TryParse(Console.ReadLine(), out inputValue))
                iterationCount = inputValue;

            Console.WriteLine("开始测试:");
            Console.WriteLine();

            string fullpath = string.Format(Path.GetFullPath(path));

            using (var db = DB.Open(fullpath, new Options { CreateIfMissing = true, WriteBufferSize = writeBufferSize }))
            {
                ulong rc = ReadRecordCount(db);

                for (int j = 0;j < iterationCount;j ++)
                {
                    Iteration(db, runTimes, insertCount, queryCount, j, rc);

                    rc += (ulong)(runTimes * insertCount);
                }

                WriteRecordCount(db, rc);
            }
        }

        private void Iteration(DB db, int runTimes, int insertCount, int queryCount, int iterator, ulong rc)
        {
            TimeSpan[] insertTimeCosts = new TimeSpan[runTimes];
            TimeSpan[] queryTimeCosts = new TimeSpan[runTimes];
            TimeSpan[] randomQueryTimeCosts = new TimeSpan[runTimes];

            for (int i = 0; i < runTimes; i++)
            {
                List<byte[]> cachedKeys = new List<byte[]>();

                DateTime dt1 = DateTime.Now;
                TestInsert(db, insertCount, cachedKeys);

                DateTime dt2 = DateTime.Now;
                insertTimeCosts[i] = dt2 - dt1;
                TestCachedQuery(db, queryCount, cachedKeys); 

                DateTime dt3 = DateTime.Now;
                queryTimeCosts[i] = dt3 - dt2;
                TestRandomQuery(db, queryCount);
                randomQueryTimeCosts[i] = DateTime.Now - dt3;
            }

            Console.WriteLine("Iteration {0}:", iterator);
            PrintRecordCount(rc);
            PrintTimeCost("Insert time cost,      ", insertTimeCosts);
            PrintTimeCost("Query time cost,       ", queryTimeCosts);
            PrintTimeCost("Random query time cost,", randomQueryTimeCosts);
            Console.WriteLine();
        }

        private ulong ReadRecordCount(DB db)
        {
            using (Snapshot snapshot = db.GetSnapshot())
            {
                ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };
                if (db.TryGet(options, "RecordCount", out Slice slice))
                {
                    return slice.ToUInt64();
                }
            }
            return 0;
        }

        private void WriteRecordCount(DB db, ulong count)
        {
            WriteBatch batch = new WriteBatch();

            batch.Put("RecordCount", count);

            db.Write(WriteOptions.Default, batch);
        }

        private void TestInsert(DB db, int insertCount, List<byte[]> cachedKeys)
        {
            WriteBatch batch = new WriteBatch();

            for (int i = 0; i < insertCount; i++)
            {
                byte[] key = GetTestKey();
                byte[] value = GetTestValue();

                cachedKeys.Add(key);
                batch.Put(key, value);
            }

            db.Write(WriteOptions.Default, batch);
        }

        private void TestRandomQuery(DB db, int queryCount)
        {
            using (Snapshot snapshot = db.GetSnapshot())
            {
                ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };

                int cached = 0;
                for (int i = 0; i < queryCount; i++)
                {
                    if (db.TryGet(options, GetTestKey(), out Slice slice))
                        cached++;
                }
            }
        }

        private int TestCachedQuery(DB db, int queryCount, List<byte[]> cachedKeys)
        {
            using (Snapshot snapshot = db.GetSnapshot())
            {
                ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };

                int hitted = 0;
                int keyCount = cachedKeys.Count;
                for (int i = 0; i < queryCount; i++)
                {
                    if (db.TryGet(options, cachedKeys[i % keyCount], out Slice slice))
                        hitted++;
                }

                return hitted;
            }
        }

        private byte[] GetTestKey()
        {
            byte[] key = new byte[32];
            rng.GetBytes(key);
            return key;
        }

        private byte[] GetTestValue()
        {
            int size = rand.Next(200, 300);
            byte[] value = new byte[size];
            rng.GetBytes(value);
            return value;
        }

        private void PrintRecordCount(ulong count)
        {
            if (count >= 1_000_000_000_000)
            {
                Console.WriteLine("Record count:{0}T", count * 0.000_000_000_001);
            }
            else if (count >= 1_000_000_000)
            {
                Console.WriteLine("Record count:{0}B", count * 0.000_000_001);
            }
            else if (count >= 1_000_000)
            {
                Console.WriteLine("Record count:{0}M", count * 0.000_001);
            }
            else if (count >= 1_000)
            {
                Console.WriteLine("Record count:{0}K", count * 0.001);
            }
            else
            {
                Console.WriteLine("Record count:{0}", count);
            }
        }

        private void PrintTimeCost(string title, TimeSpan[] timeCosts)
        {
            Console.WriteLine("{0} total:{1}ms, average:{2}ms, min:{3}ms, max:{4}ms", title, 
                timeCosts.Sum(p => p.TotalMilliseconds), timeCosts.Average(p => p.TotalMilliseconds), 
                timeCosts.Min(p => p.TotalMilliseconds), timeCosts.Max(p => p.TotalMilliseconds));
        }
    }
}
