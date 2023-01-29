using GuardRecord;
using System;
using System.Collections.Generic;
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

#if DEBUG
        private static Logger _logger = new("./Logs/");
#else
        private static Logger _logger = new("../logs/");
#endif
        private static Dictionary<string, BilibiliClient> _roomList = new();

        static void Main(string[] _) {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            _logger.OnWriteLog += Logger_WriteLog;

            _logger.Info("System", "程序启动...");
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = $"舰队记录工具 - V{VERSION}";

            Task.Run(async () => {
                //throw new UnauthorizedAccessException("The header part of a frame could not be read.");
                while(true) {
                    var rooms = Http.GetJson("http://uptools.moegarden.com/api/app/rooms");
                    if(rooms["code"].ToString() == "0") {
                        var roomIds = rooms["data"]["list"].Select(r => r["roomId"].ToString());
                        foreach(var roomId in roomIds) {
                            if(_roomList.ContainsKey(roomId)) continue;
                            _logger.Info("System", $"连接直播间[{roomId}]...");
                            var client = new BilibiliClient(roomId);
                            client.GuardBuy += LiveRoom_GuardBuy;
                            client.Connect();
                            _roomList.Add(roomId, client);
                            _logger.Info("System", $"连接成功, 开始监听[{roomId}]...");
                        }
                        foreach(var roomId in _roomList.Keys) {
                            if(roomIds.Contains(roomId)) continue;
                            _roomList[roomId].GuardBuy -= LiveRoom_GuardBuy;
                            _roomList[roomId].Dispose();
                            _roomList.Remove(roomId);
                        }
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
#if !DEBUG
            Http.Post("http://uptools.moegarden.com/api/app/history", "{" +
                $"\"room_id\":\"{client.RoomId}\"," +
                $"\"user_id\":\"{e.UserId}\"," +
                $"\"username\":\"{e.Username}\"," +
                $"\"level\":\"{e.Level}\"," +
                $"\"num\":\"{e.Number}\"" +
                "}");
#endif
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
