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

        private string path = "leveldb_test";

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
            TimeSpan[] queryCachedSnapshotTimeCosts = new TimeSpan[runTimes];
            TimeSpan[] queryRandomSnapshotTimeCosts = new TimeSpan[runTimes];
            TimeSpan[] queryCachedDirectTimeCosts = new TimeSpan[runTimes];
            TimeSpan[] queryRandomDirectTimeCosts = new TimeSpan[runTimes];

            List<byte[]> randomKeys = new List<byte[]>();
            List<byte[]> randomValues = new List<byte[]>();
            List<byte[]> randomQueryKeys = new List<byte[]>();

            int keyCount = runTimes * insertCount;
            GenerateRandomKeyValues(randomKeys, randomQueryKeys, randomValues, keyCount);            

            for (int i = 0; i < runTimes; i++)
            {
                List<byte[]> cachedKeys = new List<byte[]>();

                insertTimeCosts[i] = TestInsert(db, insertCount, i, randomKeys, randomValues);

                queryCachedSnapshotTimeCosts[i] = TestQuerySnapshot(db, queryCount, i, randomKeys, keyCount);
                queryRandomSnapshotTimeCosts[i] = TestQuerySnapshot(db, queryCount, i, randomQueryKeys, keyCount);

                queryCachedDirectTimeCosts[i] = TestQueryDirect(db, queryCount, i, randomKeys, keyCount);
                queryRandomDirectTimeCosts[i] = TestQueryDirect(db, queryCount, i, randomQueryKeys, keyCount);
            }

            Console.WriteLine("Iteration {0}:", iterator);
            PrintRecordCount(rc);
            PrintTimeCost("Insert time cost,               ", insertTimeCosts);
            PrintTimeCost("Query cached snapshot time cost,", queryCachedSnapshotTimeCosts);
            PrintTimeCost("Random query snapshot time cost,", queryRandomSnapshotTimeCosts);
            PrintTimeCost("Query cached direct time cost,  ", queryCachedDirectTimeCosts);
            PrintTimeCost("Random query direct time cost,  ", queryRandomDirectTimeCosts);
            Console.WriteLine();
        }

        private void GenerateRandomKeyValues(List<byte[]> randomKeys, List<byte[]> randomQueryKeys, List<byte[]> randomValues, int count)
        {
            for (int i = 0;i <count;i ++)
            {
                randomKeys.Add(GetRandomKey());
                randomQueryKeys.Add(GetRandomKey());
                randomValues.Add(GetRandomValue());
            }
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

        private TimeSpan TestInsert(DB db, int insertCount, int runIndex, List<byte[]> keys, List<byte[]> values)
        {
            DateTime dt = DateTime.Now;

            WriteBatch batch = new WriteBatch();

            for (int i = 0; i < insertCount; i++)
            {
                int index = runIndex * insertCount + i;
                batch.Put(keys[index], values[index]);
            }

            db.Write(WriteOptions.Default, batch);

            return DateTime.Now - dt;
        }

        private TimeSpan TestQuerySnapshot(DB db, int queryCount, int runIndex, List<byte[]> keys, int keyCount)
        {
            DateTime dt = DateTime.Now;

            using (Snapshot snapshot = db.GetSnapshot())
            {
                ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };

                int hitted = 0;
                for (int i = 0; i < queryCount; i++)
                {
                    int index = runIndex * queryCount + i;
                    if (db.TryGet(options, keys[index % keyCount], out Slice slice))
                        hitted++;
                }
            }

            return DateTime.Now - dt;
        }

        private TimeSpan TestQueryDirect(DB db, int queryCount, int runIndex, List<byte[]> keys, int keyCount)
        {
            DateTime dt = DateTime.Now;

            int hitted = 0;
            for (int i = 0; i < queryCount; i++)
            {
                int index = runIndex * queryCount + i;
                if (db.TryGet(ReadOptions.Default, keys[index % keyCount], out Slice slice))
                    hitted++;
            }
            return DateTime.Now - dt;
        }

        private byte[] GetRandomKey()
        {
            byte[] key = new byte[32];
            rng.GetBytes(key);
            return key;
        }

        private byte[] GetRandomValue()
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
