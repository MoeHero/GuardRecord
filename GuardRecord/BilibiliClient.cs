using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp.NetCore;

namespace GuardRecord
{
    public class BilibiliClient
    {
        private const int LIVE_TYPE_HEARTBEAT = 2;
        private const int LIVE_TYPE_ENTER_ROOM = 7;
        private const string WEBSOCKET_URL = "ws://broadcastlv.chat.bilibili.com:2244/sub";

        private readonly WebSocket _webSocket = new(WEBSOCKET_URL);
        private readonly Timer _heartbeatTimer = new(30000);
        private bool _connected;

        public event EventHandler<GuardBuyEventArgs> GuardBuy;
        public event EventHandler<OtherEventArgs> OtherEvent;

        public string RoomId { get; }

        public BilibiliClient(string roomId) {
            RoomId = roomId;
            _webSocket.OnMessage += OnMessage;
            _webSocket.OnOpen += OnOpen;
            _webSocket.OnClose += OnClose;
            _heartbeatTimer.Elapsed += (sender, e) => SendData(LIVE_TYPE_HEARTBEAT);
        }

        public void Connect() {
            _webSocket.Connect();
        }

        private void OnMessage(object sender, MessageEventArgs e) {
            ParseData(e.RawData);
        }

        private void OnOpen(object sender, EventArgs e) {
            _connected = true;
            SendData(LIVE_TYPE_ENTER_ROOM, $"{{\"uid\":0,\"roomid\":{RoomId}}}");
            _heartbeatTimer.Start();
            SendData(LIVE_TYPE_HEARTBEAT);
        }

        private void OnClose(object sender, CloseEventArgs e) {
            _connected = false;
            _heartbeatTimer.Stop();
            Connect();
        }
        
        public void Dispose() {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
            _webSocket.OnOpen -= OnOpen;
            _webSocket.OnClose -= OnClose;
            _webSocket.OnMessage -= OnMessage;
            _webSocket.CloseAsync();
        }

        private async void ParseData(byte[] data) {
            var dataLength = data.Length;
            var packetIndex = 0;
            var danmaku = DanmakuProtocol.FromBytes(data);

            do {
                if(danmaku.Type == 5 && danmaku.Data.Length > 2) {
                    if(danmaku.ReadInt16() == 0x78DA) { //使用GZIP压缩
                        using var compressStream = new MemoryStream(danmaku.Data, 2, danmaku.Data.Length - 2);
                        using var gzipStream = new DeflateStream(compressStream, CompressionMode.Decompress, true);
                        using var memoryStream = new MemoryStream();
                        byte[] _data;

                        await gzipStream.CopyToAsync(memoryStream);
                        _data = memoryStream.ToArray();
                        var _dataLength = _data.Length;
                        var _packetIndex = 0;
                        var _danmaku = DanmakuProtocol.FromBytes(_data);

                        do {
                            ParseDanmu(Encoding.UTF8.GetString(_danmaku.Data, 0, _danmaku.Data.Length));

                            _packetIndex += _danmaku.PacketLength;
                            if(_dataLength - _packetIndex < 16) break;
                            _danmaku = DanmakuProtocol.FromBytes(_data, _packetIndex);
                        } while(true);
                    } else {
                        ParseDanmu(Encoding.UTF8.GetString(danmaku.Data, 0, danmaku.Data.Length));
                    }
                }

                packetIndex += danmaku.PacketLength;
                if(dataLength - packetIndex < 16) break;
                danmaku = DanmakuProtocol.FromBytes(data, packetIndex);
            } while(true);
        }

        private void ParseDanmu(string danmakuJson) {
            //忽略弹幕
            if(danmakuJson.StartsWith("{\"cmd\":\"DANMU_MSG\"")) return;
            var danmaku = JObject.Parse(danmakuJson);
            switch(danmaku["cmd"].Value<string>()) {
                case "GUARD_BUY": { //3舰长 2提督 1总督
                    var level = danmaku["data"]["guard_level"].Value<int>();
                    var number = danmaku["data"]["num"].Value<int>();
                        var username = danmaku["data"]["username"].Value<string>();
                        var userId = danmaku["data"]["uid"].Value<string>();
                        GuardBuy?.Invoke(this, new GuardBuyEventArgs { Level = level, Number = number, Username = username, UserId = userId });
                    break;
                }
                default: {
                    OtherEvent?.Invoke(this, new OtherEventArgs { JsonData = danmakuJson });
                    break;
                }
            }
        }

        private async void SendData(int type, string data = "") {
            if(!_connected) return;
            _webSocket.Send(await new DanmakuProtocol {
                Type = type,
                Data = Encoding.UTF8.GetBytes(data),
            }.ToBytes());
        }
    }

    public class GuardBuyEventArgs : EventArgs
    {
        /// <summary>
        /// 等级 1.总督 2.提督 3.舰长
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// 赠送人Id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 赠送人昵称
        /// </summary>
        public string Username { get; set; }
    }

    public class OtherEventArgs : EventArgs
    {
        /// <summary>
        /// Json数据
        /// </summary>
        public string JsonData { get; set; }
    }

    public class DanmakuProtocol
    {
        /// <summary>
        /// 总长度 (协议头 + 数据长度)
        /// </summary>
        public int PacketLength { get => HeaderLength + Data.Length; }

        /// <summary>
        /// 头长度 (固定为16)
        /// </summary>
        public short HeaderLength { get; private set; } = 16;

        /// <summary>
        /// 版本
        /// </summary>
        public short Version { get; private set; } = 2;

        /// <summary>
        /// 类型
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 序列ID
        /// </summary>
        public int SequenceId { get; set; } = 1;

        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Data { get; set; }

        public static DanmakuProtocol FromBytes(byte[] data, int offset = 0) {
            if(data.Length < 16) throw new ArgumentOutOfRangeException(nameof(data), $"{nameof(data)}的长度必须大于16位");
            if(offset + 16 > data.Length) throw new ArgumentOutOfRangeException(nameof(offset), $"必须确保{nameof(offset)}后{nameof(data)}的长度仍有16位");

            var r = new DanmakuProtocol();
            var packetLength = EndianBitConverter.EndianBitConverter.BigEndian.ToInt32(data, offset + 0);
            r.HeaderLength = EndianBitConverter.EndianBitConverter.BigEndian.ToInt16(data, offset + 4);
            r.Version = EndianBitConverter.EndianBitConverter.BigEndian.ToInt16(data, offset + 6);
            r.Type = EndianBitConverter.EndianBitConverter.BigEndian.ToInt32(data, offset + 8);
            r.SequenceId = EndianBitConverter.EndianBitConverter.BigEndian.ToInt32(data, offset + 12);
            if(packetLength - r.HeaderLength > 0) {
                r.Data = new byte[packetLength - r.HeaderLength];
                Array.Copy(data, offset + r.HeaderLength, r.Data, 0, r.Data.Length);
            }
            return r;
        }

        public async Task<byte[]> ToBytes() {
            using var memoryStream = new MemoryStream(PacketLength);
            await memoryStream.WriteAsync(EndianBitConverter.EndianBitConverter.BigEndian.GetBytes(PacketLength).AsMemory(0, 4));
            await memoryStream.WriteAsync(EndianBitConverter.EndianBitConverter.BigEndian.GetBytes(HeaderLength).AsMemory(0, 2));
            await memoryStream.WriteAsync(EndianBitConverter.EndianBitConverter.BigEndian.GetBytes(Version).AsMemory(0, 2));
            await memoryStream.WriteAsync(EndianBitConverter.EndianBitConverter.BigEndian.GetBytes(Type).AsMemory(0, 4));
            await memoryStream.WriteAsync(EndianBitConverter.EndianBitConverter.BigEndian.GetBytes(SequenceId).AsMemory(0, 4));
            if(Data?.Length > 0) await memoryStream.WriteAsync(Data);
            return memoryStream.ToArray();
        }

        public short ReadInt16(int startIndex = 0) {
            return EndianBitConverter.EndianBitConverter.BigEndian.ToInt16(Data, startIndex);
        }
    }
}
