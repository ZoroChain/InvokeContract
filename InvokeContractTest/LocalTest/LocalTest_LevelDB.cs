using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Zoro.IO.Data.LevelDB;

namespace InvokeContractTest
{
    class LocalTest_LevelDB : IExample
    {
        public string Name => "测试LevelDB的性能";
        private string path = Directory.GetCurrentDirectory();
        private List<string> keyCache = new List<string>();

        public async Task StartAsync()
        {
            await Task.Run(() => Test());
        }

        private void Test()
        {
            Console.WriteLine("输入要进行的操作编号：1.插入数据;2.查询数据;3.删除数据;");
            var typeNumber = Console.ReadLine();
            switch (typeNumber) {
                case "1":
                    if (!Directory.Exists(path + "//leveldb"))
                    {
                        Directory.CreateDirectory(path + "//leveldb");
                    }
                    Console.WriteLine("输入要插入多少数据（单位：万）:");
                    var dbNumber = Console.ReadLine();
                    Console.WriteLine("输入每插入多少条数据，输出用时：");
                    var num = Console.ReadLine();
                    var db = DB.Open(path + "//leveldb", new Options { CreateIfMissing = true});
                    System.Diagnostics.Stopwatch Totalsp = new System.Diagnostics.Stopwatch();
                    Totalsp.Reset();
                    Totalsp.Start();
                    System.Diagnostics.Stopwatch sp = new System.Diagnostics.Stopwatch();
                    sp.Reset();
                    sp.Start();
                    long mCount = 0;
                    long length = long.Parse(dbNumber + "0000");
                    int consoleNumber = int.Parse(num);
                    WriteBatch batch = new WriteBatch();                   
                    keyCache = new List<string>(100000000);
                    string s = null;
                    while (mCount < length) {
                        s = Guid.NewGuid().ToString();
                        batch.Put(s, Message + mCount);
                        keyCache.Add(s);
                        if (System.Threading.Interlocked.Increment(ref mCount) % consoleNumber == 0) {
                            var startTime = DateTime.Now;
                            db.Write(WriteOptions.Default, batch);
                            TimeSpan span = DateTime.Now - startTime;
                            Console.WriteLine($"耗时:{new DateTime(span.Ticks).TimeOfDay:hh\\:mm\\:ss\\.fff}");
                            batch.Clear();                           
                            Console.WriteLine("{0} has inserted. time use {1}ms.", mCount, sp.ElapsedMilliseconds);
                            sp.Reset();
                            sp.Start();
                        }
                    }
                    Console.WriteLine("{0} has inserted. time use {1}ms.", mCount, Totalsp.ElapsedMilliseconds);
                    db.Dispose();
                    break;
                case "2":
                    if (!Directory.Exists(path + "//leveldb"))
                    {
                        Console.WriteLine("leveldb里面没有存储数据。");
                    }
                    else if (keyCache.Count == 0)
                    {
                        Console.WriteLine("没有在缓存中的key值，请先操作插入数据。");
                    }
                    else
                    {
                        var selectDB = DB.Open(path + "//leveldb", new Options { CreateIfMissing = true });
                        Console.WriteLine("输入查询数据方式：1.顺序查询;2.随机查询;");
                        var selectType = Console.ReadLine();
                        switch (selectType) {
                            case "1":
                                System.Diagnostics.Stopwatch selectSP = new System.Diagnostics.Stopwatch();
                                selectSP.Reset();
                                selectSP.Start();
                                Console.WriteLine("根据自己插入的数据量，输入起始index:");
                                var start = Console.ReadLine();
                                Console.WriteLine("根据自己插入的数据量，输入结束index:");
                                var end = Console.ReadLine();
                                int startNum = int.Parse(start);
                                int endNum = int.Parse(end);
                                if (startNum < 0 || endNum < 0 || startNum > endNum || startNum > keyCache.Count - 1 || endNum > keyCache.Count - 1) {
                                    Console.WriteLine("参数输入错误。");
                                }
                                else
                                {
                                    while (System.Threading.Interlocked.Increment(ref startNum) <= endNum)
                                    {
                                        Console.WriteLine("selected message {0} . time use {1}ms.", selectDB.Get(ReadOptions.Default, keyCache[startNum]).ToString(), selectSP.ElapsedMilliseconds);
                                    }
                                }                                                            
                                break;
                            case "2":
                                System.Diagnostics.Stopwatch selectSP1 = new System.Diagnostics.Stopwatch();
                                selectSP1.Reset();
                                selectSP1.Start();
                                Console.WriteLine("根据自己插入的数据量，输入随机查询数值:");
                                var random = Console.ReadLine();
                                var randomNum = int.Parse(random);
                                int incrementNum = 0;
                                if (randomNum < 0 || randomNum > keyCache.Count - 1)
                                {
                                    Console.WriteLine("参数输入错误。");
                                }
                                else
                                {
                                    while (System.Threading.Interlocked.Increment(ref incrementNum) < randomNum)
                                    {
                                        Console.WriteLine("selected message {0} . time use {1}ms.", selectDB.Get(ReadOptions.Default, keyCache[new Random().Next(keyCache.Count - 1)]).ToString(), selectSP1.ElapsedMilliseconds);
                                    }
                                }
                                break;
                            default:
                                Console.WriteLine("输入编号不存在，请重新输入。");
                                break;
                        }
                        selectDB.Dispose();
                    }                  
                    break;
                case "3":
                    if (Directory.Exists(path + "//leveldb")) {
                        DeleteFolder(path + "//leveldb");
                        Console.WriteLine("leveldb数据删除完毕");
                    }
                    break;
                default:
                    Console.WriteLine("输入编号不存在，请重新输入。");
                    break;
            }

           
           // Console.WriteLine($"耗时:{new DateTime(span.Ticks).TimeOfDay:hh\\:mm\\:ss\\.fff}");
        }

        private void DeleteFolder(string dir) {
            foreach (string s in Directory.GetFileSystemEntries(dir)) {
                if (File.Exists(s))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(s);
                        if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                        {
                            fi.Attributes = FileAttributes.Normal;
                        }
                        File.Delete(s);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else {
                    try
                    {
                        DirectoryInfo d = new DirectoryInfo(s);
                        if (d.GetFiles().Length != 0) {
                            DeleteFolder(d.FullName);
                        }
                        Directory.Delete(s);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                Directory.Delete(dir);
            }
        }

        private string Message = "{\"jsonrpc\": \"2.0\", \"id\": \"1\",\"result\": {\"hash\": \"0xf87ed6ebc4b2d17c9466d7b624be1f536e213138b939aab703c5dbdf891c4ef3\",\"size\": 4591,\"version\": 0," +
            "\"previousblockhash\": \"0x665484494cf95365eabf973c137bf73306fd78b406790005e80e8b70a2ba6d47\",\"merkleroot\": \"0x8bb9752837762534fbc414449ec9d7bf24c78e344368d69ac330667995ae648c\"," +
                "\"time\": 1545713823,\"index\": 16,\"nonce\": \"35287d415656bda0\",\"nextconsensus\": \"ASo5o2RJ3LbWVf8bsnqe1ncWdpY7ih3qqb\",\"script\": {"+
                    "\"invocation\":\"40fe58ff460aa98540c955867107bb319fb56813fc781ad48fdbdd74ca9219889407d3d402cab52a7d85b2d3757982f5d2f2b884b84e4b2f3e26f0153e29e36a5540dc703aaa3d76c899b28cbd9c3268318c20a672fa30381ba106ec1d66c604d4974605b283ddffdfb0867a24b8e0c31634228562939c89530917db69082abbd6b240b9709fc3223a247bacb2a4af86321bb5e5c62fab9f39651915de51dd8ae286d9b9634519ab8294a76b8886e24a87367fa0cef78df42be505acccf4e270237418\","+
                    "\"verification\": \"5321025178aa02ccb9a30c74c0e9771ed60d771710625e41d1ae37a192a6db2c00e7d62102ade1a21bd7d90b88299e7e1fef91c12fdc9988ad9100816d3b50cb6090fd88a22102f0a7538d3aa6fc6a91315c5842733820df5b4ec1e4f273adc5d36eebd0f7463a2103f35c16c5e8837697b9263f44f62be58c058562e76b033ed29a2223792901f6b154ae\""+
                "},\"tx\": [{\"txid\": \"0xbf9c5ba645678b898866d09e44677f2f83a7f7d93018020abe786eae9b2646a4\",\"size\": 28,\"type\": \"MinerTransaction\",\"version\": 1,\"attributes\": [],"+
                        "\"sys_fee\": \"0\",\"net_fee\": \"0\",\"scripts\": [],\"nonce\": 1448525216,\"address\": \"AbE6cCQGstikD1QvwTnkrD3Jid6JGPY4oq\"},{\"txid\": \"0xf1cdc1f7afcf5544b4afbd262840e04281618f714cc7a7d9decd9b7ca64c7028\","+
                        "\"size\": 4121, \"type\": \"InvocationTransaction\",\"version\": 2,\"attributes\": [],\"sys_fee\": \"500\",\"net_fee\": \"500\",\"scripts\": [{"+
                                "\"invocation\": \"40ca6f2bc33347f05978b4544073674b41a5958c12707e9e0ba259fe8d104d88b9fa3a4e1b9281bd7abd1838bb146629638944d392e2a3b850e7c4e53f73bf1cc1\","+
                                "\"verification\": \"2102dc342e85b6bf60fcce7835acd27051c19e15a73e0567130c7563dba087d1ce0dac\"}],"+
                        "\"script\": \"01300130024c5a03312e30056d796761735501050207104d580f0138c56b6c766b00527ac46c766b51527ac461086263702d746573746c766b52527ac46168164e656f2e"+
            "626c656c766b53527ac46203006c766b53c3616c756668145a6f726f2e436f6e74726163742e437265617465\","+
                        "\"gas_limit\": \"500\",\"gas_price\": \"1\",\"script_hash\": \"0x42d5b342b62262ba2f7fcbfadaf9f74db4564b0c\"}],"+
                "\"confirmations\": 1634,\"nextblockhash\": \"0xc04d89d15ada45575bb494c00685bbb7d86fd9509115fe10233597a59db2da80\"}}";
    }
}
