using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;

namespace NetStash.Storage.Proxy
{
    public class BaseProxy
    {
        private static string dbFilePath = "./NetStash.db";
        private static bool initialized = false;
        private static object _lock = new object();

        public BaseProxy()
        {
            lock (_lock)
            {
                if (initialized) return;

                if (!string.IsNullOrWhiteSpace(this.BaseDirectory))
                {
                    Directory.CreateDirectory(Path.GetFullPath(this.BaseDirectory));
                    System.Environment.SetEnvironmentVariable("PATH", $"{System.Environment.GetEnvironmentVariable("PATH")};{Path.GetFullPath(this.BaseDirectory)}");
                }

                if (!File.Exists(Path.Combine(this.BaseDirectory, "SQLite.Interop.dll")))
                {
                    var resourceName = $"NetStash.{(System.Environment.Is64BitOperatingSystem ? "x64" : "x86")}.SQLite.Interop.dll";
                    this.SaveToDisk(resourceName, Path.Combine(this.BaseDirectory, "SQLite.Interop.dll"));
                }

                if (!File.Exists(this.DbFilePath))
                {
                    System.Data.SQLite.SQLiteConnection.CreateFile(this.DbFilePath);

                    using (var cnn = (System.Data.SQLite.SQLiteConnection)this.GetConnection())
                    {
                        cnn.Open();
                        var cmd = new System.Data.SQLite.SQLiteCommand("CREATE TABLE \"Log\" ([IdLog] integer, [Message] nvarchar, PRIMARY KEY(IdLog));", cnn);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var cnn = (System.Data.SQLite.SQLiteConnection)this.GetConnection())
                    using (System.Data.SQLite.SQLiteCommand command = cnn.CreateCommand())
                    {
                        command.CommandText = "vacuum;";
                        command.ExecuteNonQuery();
                    }
                }

                initialized = true;
            }
        }

        internal string BaseDirectory => ConfigurationManager.AppSettings["NetStash.BaseDirectory"] ?? string.Empty;

        internal string DbFilePath => Path.GetFullPath(Path.Combine(this.BaseDirectory, dbFilePath));

        internal IDbConnection GetConnection()
        {
            return new System.Data.SQLite.SQLiteConnection(string.Format("Data Source={0};Version=3;", this.DbFilePath));
        }

        private void SaveToDisk(string file, string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file))
            using (var fileStream = new FileStream(name, FileMode.CreateNew))
                for (int i = 0; i < stream.Length; i++)
                    fileStream.WriteByte((byte)stream.ReadByte());
        }
    }
}
