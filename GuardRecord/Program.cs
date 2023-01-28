using dotenv.net;
using dotenv.net.Utilities;
using GuardRecord;
using GuardRecord.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: AssemblyVersion(Program.VERSION)]
[assembly: AssemblyFileVersion(Program.VERSION)]
[assembly: AssemblyInformationalVersion(Program.VERSION)]
[assembly: AssemblyTitle("舰队记录工具")]
[assembly: AssemblyProduct("舰队记录工具")]
[assembly: AssemblyCompany("MoeGarden")]

namespace GuardRecord
{
    class Program
    {
        public const string VERSION = "0.1.0";

        private readonly static IFreeSql _db = new FreeSql.FreeSqlBuilder()
            .UseConnectionString(FreeSql.DataType.MySql, @"data source=10.0.0.2;port=3306;user id=uptools;password=frj*fza-rwu3qmk6DKM;initial catalog=uptools;charset=utf8")
            .Build();

        private static Logger _logger = null;
        private static Dictionary<string, BilibiliClient> _roomList = new();
        private static List<Rooms> _dbRooms = new();

        static void Main(string[] _) {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            DotEnv.Load();
            _logger = new(EnvReader.GetStringValue("LOG_PATH"));
            _logger.OnWriteLog += Logger_WriteLog;

            _logger.Info("System", "程序启动...");
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = $"舰队记录工具 - V{VERSION}";

            Task.Run(async () => {
                while(true) {
                    _dbRooms = await _db.Select<Rooms>().Where(r => r.IsDeleted == 0).ToListAsync();
                    foreach(var roomId in _dbRooms.Select(r => r.RoomId)) {
                        if(_roomList.ContainsKey(roomId)) continue;
                        _logger.Info("System", $"连接直播间[{roomId}]...");
                        var client = new BilibiliClient(roomId);
                        client.GuardBuy += LiveRoom_GuardBuy;
                        client.Connect();
                        _roomList.Add(roomId, client);
                        _logger.Info("System", $"连接成功, 开始监听[{roomId}]...");
                    }
                    foreach(var roomId in _roomList.Keys) {
                        if(_dbRooms.Select(r => r.RoomId).Contains(roomId)) continue;
                        _roomList[roomId].GuardBuy -= LiveRoom_GuardBuy;
                        _roomList[roomId].Dispose();
                        _roomList.Remove(roomId);
                    }
                    await Task.Delay(60000);
                }
            });

            Thread.Sleep(Timeout.Infinite);
        }

        private static void LiveRoom_GuardBuy(object sender, GuardBuyEventArgs e) {
            var client = sender as BilibiliClient;
            var levelName = e.Level switch {
                1 => "总督",
                2 => "提督",
                3 => "舰长",
                _ => $"未知:{e.Level}"
            };
            _logger.Info("LiveRoom", $"舰队开通 房间号:{client.RoomId} Uid:{e.UserId} 用户名:{e.Username} 等级:{levelName} 数量:{e.Number}");
            var levelEnum = e.Level switch {
                1 => GuardHistoriesLEVEL.Unknow1,
                2 => GuardHistoriesLEVEL.Unknow2,
                3 => GuardHistoriesLEVEL.Unknow3,
            };
            _db.Insert(new GuardHistories {
                RoomId = _dbRooms.First(r => r.RoomId == client.RoomId).Id,
                UserId = e.UserId,
                Username = e.Username,
                Level = levelEnum,
                Num = e.Number,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            }).ExecuteAffrows();
        }

        private static void Logger_WriteLog(object sender, LogEventArgs e) {
            if(e.Level == LogLevel.Debug) return;
            Console.WriteLine($"[{e.Level}][{e.Time:yyyy-MM-dd HH:mm:ss.fff}][{e.Name}] {e.Log}");
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {
            _logger.ErrorReport(e.ExceptionObject, e.IsTerminating);
            Console.WriteLine($"[ERROR][{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {e.ExceptionObject}");
        }
    }
}
