using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Zoro.IO.Data.LevelDB;
using System.Threading;

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
                    var db = DB.Open(path + "//leveldb", new Options { CreateIfMissing = true, Compression = CompressionType.kSnappyCompression, WriteBufferSize = int.Parse(Math.Pow(2, 24) + "")});
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
                    Random r = new Random();                    
                    string s = null;
                    while (mCount < length) {
                        s = Guid.NewGuid().ToString();
                        byte[] testkey = System.Text.Encoding.UTF8.GetBytes(s);
                        byte[] testv = new byte[128];
                        r.NextBytes(testv);
                        batch.Put(testkey, testv);
                        //db.Put(WriteOptions.Default, testkey, testv);
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
                            Thread.Sleep(300);
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
                                    System.Diagnostics.Stopwatch selectSP = new System.Diagnostics.Stopwatch();
                                    selectSP.Reset();
                                    selectSP.Start();
                                    ReadOptions readOptions = new ReadOptions { FillCache = false };
                                    while (System.Threading.Interlocked.Increment(ref startNum) <= endNum)
                                    {
                                        Console.WriteLine("selected message {0} . time use {1}ms.", selectDB.Get(readOptions, keyCache[startNum]).ToString(), selectSP.ElapsedMilliseconds);
                                    }
                                }                                                            
                                break;
                            case "2":                              
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
                                    System.Diagnostics.Stopwatch selectSP1 = new System.Diagnostics.Stopwatch();
                                    selectSP1.Reset();
                                    selectSP1.Start();
                                    ReadOptions readOptions = new ReadOptions { FillCache = false };
                                    while (System.Threading.Interlocked.Increment(ref incrementNum) < randomNum)
                                    {
                                        Console.WriteLine("selected message {0} . time use {1}ms.", selectDB.Get(readOptions, keyCache[new Random().Next(keyCache.Count - 1)]).ToString(), selectSP1.ElapsedMilliseconds);
                                        Console.WriteLine($"耗时:{new DateTime(selectSP1.ElapsedTicks).TimeOfDay:hh\\:mm\\:ss\\.fffff}");
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
            }
            Directory.Delete(dir);
        }
    }
}
