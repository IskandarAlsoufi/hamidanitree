using System;

namespace HamidaniTree.Model
{
    public static class MySQLConnectionGenerator
    {
        public static void Initialize(string host,string userName,string port, string password)
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException($"'{nameof(userName)}' cannot be null or empty.", nameof(userName));
            }

            if (string.IsNullOrEmpty(port))
            {
                throw new ArgumentException($"'{nameof(port)}' cannot be null or empty.", nameof(port));
            }

            Host = host;
            UserName = userName;
            Password = password;
            Port = port;
        }

        public static string  Host { get; private set; }
        public static string  UserName { get; private set; }
        public static string Password { get; private set; }
        public static string Port { get; private set; }
        public static string ConnectionString(string db_name)
        {
            return  $"Server={Host};Database={db_name};User={UserName};Port={Port};Password={Password};SslMode=none";
        }
    }
}
