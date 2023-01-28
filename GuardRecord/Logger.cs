using System;
using System.IO;
using System.Text;

namespace GuardRecord
{
    internal class Logger : IDisposable
    {
        private readonly string Path;
        private FileStream LogFile;
        private string LastFilename;

        public event EventHandler<LogEventArgs> OnWriteLog;

        public Logger(string path) {
            if(!Directory.Exists(path)) Directory.CreateDirectory(path);
            Path = path;
        }

        public void WriteLog(LogLevel level, string name, string log) {
            var datetime = DateTime.Now;
            if(LogFile == null || LastFilename != $"{datetime:yyyy-MM-dd}.log") {
                LogFile?.Close();
                LastFilename = $"{datetime:yyyy-MM-dd}.log";
                LogFile = File.Open(Path + $"{datetime:yyyy-MM-dd}.log", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                LogFile.Seek(0, SeekOrigin.End);
            }
            var data = Encoding.UTF8.GetBytes($"[{level}][{datetime:yyyy-MM-dd HH:mm:ss.fff}][{name}] {log}{Environment.NewLine}");
            if(!LogFile.CanWrite) return;
            LogFile.Write(data, 0, data.Length);
            LogFile.Flush();
            OnWriteLog?.Invoke(null, new LogEventArgs { Level = level, Time = datetime, Name = name, Log = log });
        }

        public void Debug(string name, string log) {
            WriteLog(LogLevel.Debug, name, log);
        }

        public void Info(string name, string log) {
            WriteLog(LogLevel.Info, name, log);
        }

        public void Warning(string name, string log) {
            WriteLog(LogLevel.Warning, name, log);
        }

        public void Error(string name, string log) {
            WriteLog(LogLevel.Error, name, log);
        }

        public void Fatal(string name, string log) {
            WriteLog(LogLevel.Fatal, name, log);
        }

        public void ErrorReport(object exceptionObject, bool isTerminating = false) {
            if(exceptionObject == null) return;
            var datetime = DateTime.Now;
            var i = 1;
            string filename;
            do {
                filename = Path + $"Error-{datetime:yyyy-MM-dd}-{i++}.log";
            } while(File.Exists(filename));

            var content = new StringBuilder();
            content.AppendLine($"程序错误报告 等级:{(isTerminating ? "致命" : "错误")}");
            content.AppendLine($"时间: {datetime:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}");
            if(exceptionObject is Exception) {
                var e = exceptionObject as Exception;
                while(true) {
                    content.AppendLine($"{e.GetType()}: {e.Message}");
                    content.AppendLine(e.StackTrace);
                    if(e.InnerException == null) break;
                    content.AppendLine("内部异常:");
                    e = e.InnerException;
                }
            } else {
                content.AppendLine($"程序遇到未知异常:");
                content.AppendLine(exceptionObject.ToString());
            }
            try {
                File.WriteAllText(filename, content.ToString());
            } catch(IOException) {
                ErrorReport(exceptionObject, isTerminating);
            }
        }

        public void Dispose() {
            LogFile?.Close();
            LogFile?.Dispose();
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal,
    }

    public class LogEventArgs : EventArgs
    {
        public LogLevel Level { get; set; }

        public DateTime Time { get; set; }

        public string Name { get; set; }

        public string Log { get; set; }
    }
}
