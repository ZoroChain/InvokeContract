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
                    System.Diagnostics.Stopwatch sp = new System.Diagnostics.Stopwatch();
                    sp.Reset();
                    sp.Start();
                    long mCount = 0;
                    long length = long.Parse(dbNumber + "0000");
                    int consoleNumber = int.Parse(num);
                    WriteBatch batch = new WriteBatch();
                    string s = null;
                    while (mCount < length) {
                        s = Guid.NewGuid().ToString();
                        batch.Put(s, Message + mCount);
                        keyCache.Add(s);
                        if (System.Threading.Interlocked.Increment(ref mCount) % consoleNumber == 0) {
                            db.Write(WriteOptions.Default, batch);
                            batch.Clear();
                            Console.WriteLine("{0} has inserted. time use {1}ms.", mCount, sp.ElapsedMilliseconds);
                            sp.Reset();
                            sp.Start();
                        }
                    }
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
                Directory.Delete(s);
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
            "52756e74696d652e47657454726967676572009c6c766b53527ac46c766b53c3640f0061006c766b54527ac46253096168164e656f2e52756e74696d652e47657454726967676572519c6c766b55527ac46c766b"+
            "55c3640f0061516c766b54527ac4621d096168164e656f2e52756e74696d652e47657454726967676572609c6c766b56527ac46c766b56c364e8086161682b53797374656d2e457865637574696f6e456e67696e"+
            "652e47657443616c6c696e67536372697074486173686c766b57527ac46c766b00c30b746f74616c537570706c79876c766b58527ac46c766b58c36411006165f7086c766b54527ac4628e086c766b00c3046e61"+
            "6d65876c766b59527ac46c766b59c3641100616579086c766b54527ac46266086c766b00c30673796d626f6c876c766b5a527ac46c766b5ac3641100616572086c766b54527ac4623c086c766b00c30864656369"+
            "6d616c73876c766b5b527ac46c766b5bc3641100616561086c766b54527ac46210086c766b00c3066465706c6f79876c766b5c527ac46c766b5cc364bc01616c766b51c3c0519c009c6c766b5f527ac46c766b5f"+
            "c3640f00006c766b54527ac462cf0761140c4b56b44df7f9dafacb7f2fba6222b642b3d5426168184e656f2e52756e74696d652e436865636b5769746e657373009c6c766b60527ac46c766b60c3640e00006c76"+
            "6b54527ac46282076168164e656f2e53746f726167652e476574436f6e746578740b746f74616c537570706c79617c680f4e656f2e53746f726167652e4765746c766b5d527ac46c766b5dc3c000a06c766b0111"+
            "527ac46c766b0111c3640e00006c766b54527ac4621f07011161140c4b56b44df7f9dafacb7f2fba6222b642b3d5427e6c766b5e527ac46168164e656f2e53746f726167652e476574436f6e746578746c766b5e"+
            "c308000014bbf08ac602615272680f4e656f2e53746f726167652e507574616168164e656f2e53746f726167652e476574436f6e746578740b746f74616c537570706c7908000014bbf08ac602615272680f4e65"+
            "6f2e53746f726167652e50757461610061140c4b56b44df7f9dafacb7f2fba6222b642b3d54208000014bbf08ac602615272087472616e7366657254c168124e656f2e52756e74696d652e4e6f7469667961616c"+
            "766b00c30962616c616e63654f66876c766b0112527ac46c766b0112c3647500616c766b51c3c0519c009c6c766b0114527ac46c766b0114c3640e00006c766b54527ac462f3056c766b51c300c36c766b011352"+
            "7ac46c766b0113c3c001149c009c6c766b0115527ac46c766b0115c3640e00006c766b54527ac462bc056c766b0113c36165d7096c766b54527ac462a8056c766b00c3087472616e73666572876c766b0116527a"+
            "c46c766b0116c3649401616c766b51c3c0539c009c6c766b011a527ac46c766b011ac3640e00006c766b54527ac46261056c766b51c300c36c766b0117527ac46c766b51c351c36c766b0118527ac46c766b0117"+
            "c36c766b0118c39c6c766b011b527ac46c766b011bc3640e00516c766b54527ac4621a056c766b0117c3c00114907c907c9e6312006c766b0118c3c001149c009c620400516c766b011c527ac46c766b011cc364"+
            "0e00006c766b54527ac462dd046c766b51c352c36c766b0119527ac46c766b0117c36168184e656f2e52756e74696d652e436865636b5769746e657373009c6c766b011d527ac46c766b011dc3640e00006c766b"+
            "54527ac4628f0461682953797374656d2e457865637574696f6e456e67696e652e476574456e747279536372697074486173686c766b57c39e6c766b011e527ac46c766b011ec3640e00006c766b54527ac46241"+
            "046c766b0118c361656009009c6c766b011f527ac46c766b011fc3640e00006c766b54527ac46219046c766b0117c36c766b0118c36c766b0119c361527265af046c766b54527ac462f7036c766b00c30c747261"+
            "6e736665725f617070876c766b0120527ac46c766b0120c364a100616c766b51c3c0539c009c6c766b0124527ac46c766b0124c3640e00006c766b54527ac462ac036c766b51c300c36c766b0121527ac46c766b"+
            "51c351c36c766b0122527ac46c766b51c352c36c766b0123527ac46c766b0121c36c766b57c39e6c766b0125527ac46c766b0125c3640e00006c766b54527ac46257036c766b0121c36c766b0122c36c766b0123"+
            "c361527265ed036c766b54527ac46235036c766b00c3096765745478496e666f876c766b0126527ac46c766b0126c3644d00616c766b51c3c0519c009c6c766b0128527ac46c766b0128c3640e00006c766b5452"+
            "7ac462ed026c766b51c300c36c766b0127527ac46c766b0127c3616557076c766b54527ac462ca026c766b00c30775706772616465876c766b0129527ac46c766b0129c3649f026161140c4b56b44df7f9dafacb"+
            "7f2fba6222b642b3d5426168184e656f2e52756e74696d652e436865636b5769746e657373009c6c766b0134527ac46c766b0134c3640e00006c766b54527ac4625b026c766b51c3c0519c6310006c766b51c3c0"+
            "599c009c620400006c766b0135527ac46c766b0135c3640e00006c766b54527ac462260261682d53797374656d2e457865637574696f6e456e67696e652e476574457865637574696e6753637269707448617368"+
            "61681a4e656f2e426c6f636b636861696e2e476574436f6e74726163746168164e656f2e436f6e74726163742e4765745363726970746c766b012a527ac46c766b51c300c36c766b012b527ac46c766b012ac36c"+
            "766b012bc39c6c766b0136527ac46c766b0136c3640e00006c766b54527ac46280010207106c766b012c527ac4556c766b012d527ac4556c766b012e527ac4036263706c766b012f527ac403312e306c766b0130"+
            "527ac4095a6f726f436861696e6c766b0131527ac401306c766b0132527ac4036263706c766b0133527ac46c766b51c3c0599c6c766b0137527ac46c766b0137c3647d00616c766b51c351c36c766b012c527ac4"+
            "6c766b51c352c36c766b012d527ac46c766b51c353c36c766b012e527ac46c766b51c354c36c766b012f527ac46c766b51c355c36c766b0130527ac46c766b51c356c36c766b0131527ac46c766b51c357c36c76"+
            "6b0132527ac46c766b51c358c36c766b0133527ac4616c766b012bc36c766b012cc36c766b012dc36c766b012ec36c766b012fc36c766b0130c36c766b0131c36c766b0132c36c766b0133c361587951795a7275"+
            "51727557795279597275527275567953795872755372755579547957727554727568144e656f2e436f6e74726163742e4d69677261746575516c766b54527ac4620f0061006c766b54527ac46203006c766b54c3"+
            "616c756651c56b610b426c6143617420436f696e6c766b00527ac46203006c766b00c3616c756651c56b61034243506c766b00527ac46203006c766b00c3616c756651c56b61586c766b00527ac46203006c766b"+
            "00c3616c756651c56b616168164e656f2e53746f726167652e476574436f6e746578740b746f74616c537570706c79617c680f4e656f2e53746f726167652e4765746c766b00527ac46203006c766b00c3616c75"+
            "665ec56b6c766b00527ac46c766b51527ac46c766b52527ac4616c766b52c300a16c766b53527ac46c766b53c3640e00006c766b54527ac46231026c766b00c36c766b51c39c6c766b55527ac46c766b55c3640e"+
            "00516c766b54527ac4620c026c766b00c3c000a06c766b56527ac46c766b56c36403016101116c766b00c37e6c766b57527ac46168164e656f2e53746f726167652e476574436f6e746578746c766b57c3617c68"+
            "0f4e656f2e53746f726167652e4765746c766b58527ac46c766b58c36c766b52c39f6c766b59527ac46c766b59c3640e00006c766b54527ac46288016c766b58c36c766b52c39c6c766b5a527ac46c766b5ac364"+
            "3b006168164e656f2e53746f726167652e476574436f6e746578746c766b57c3617c68124e656f2e53746f726167652e44656c65746561624300616168164e656f2e53746f726167652e476574436f6e74657874"+
            "6c766b57c36c766b58c36c766b52c394615272680f4e656f2e53746f726167652e5075746161616c766b51c3c000a06c766b5b527ac46c766b5bc3648a006101116c766b51c37e6c766b5c527ac46168164e656f"+
            "2e53746f726167652e476574436f6e746578746c766b5cc3617c680f4e656f2e53746f726167652e4765746c766b5d527ac46168164e656f2e53746f726167652e476574436f6e746578746c766b5cc36c766b5d"+
            "c36c766b52c393615272680f4e656f2e53746f726167652e50757461616c766b00c36c766b51c36c766b52c3615272654b0061616c766b00c36c766b51c36c766b52c3615272087472616e7366657254c168124e"+
            "656f2e52756e74696d652e4e6f7469667961516c766b54527ac46203006c766b54c3616c756657c56b6c766b00527ac46c766b51527ac46c766b52527ac4616153c56c766b53527ac46c766b53c36c766b00c300"+
            "7cc46c766b53c36c766b51c3517cc46c766b53c36c766b52c3527cc46c766b53c36168154e656f2e52756e74696d652e53657269616c697a656c766b54527ac461682953797374656d2e457865637574696f6e45"+
            "6e67696e652e476574536372697074436f6e7461696e65726168174e656f2e5472616e73616374696f6e2e476574486173686c766b55527ac401136c766b55c37e6c766b56527ac46168164e656f2e53746f7261"+
            "67652e476574436f6e746578746c766b56c36c766b54c3615272680f4e656f2e53746f726167652e50757461616c756653c56b6c766b00527ac46101116c766b00c37e6c766b51527ac46168164e656f2e53746f"+
            "726167652e476574436f6e746578746c766b51c3617c680f4e656f2e53746f726167652e4765746c766b52527ac46203006c766b52c3616c756655c56b6c766b00527ac46101136c766b00c37e6c766b51527ac4"+
            "6168164e656f2e53746f726167652e476574436f6e746578746c766b51c3617c680f4e656f2e53746f726167652e4765746c766b52527ac46c766b52c3c0009c6c766b53527ac46c766b53c3640e00006c766b54"+
            "527ac4622c006c766b52c36168174e656f2e52756e74696d652e446573657269616c697a656c766b54527ac46203006c766b54c3616c756654c56b6c766b00527ac4616c766b00c361681a4e656f2e426c6f636b"+
            "636861696e2e476574436f6e74726163746c766b51527ac46c766b51c300876c766b52527ac46c766b52c3640e00516c766b53527ac4622b006c766b51c36168164e656f2e436f6e74726163742e497350617961"+
            "626c656c766b53527ac46203006c766b53c3616c756668145a6f726f2e436f6e74726163742e437265617465\","+
                        "\"gas_limit\": \"500\",\"gas_price\": \"1\",\"script_hash\": \"0x42d5b342b62262ba2f7fcbfadaf9f74db4564b0c\"}],"+
                "\"confirmations\": 1634,\"nextblockhash\": \"0xc04d89d15ada45575bb494c00685bbb7d86fd9509115fe10233597a59db2da80\"}}";
    }
}
