using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SRIMAK
{
    public class DBConnection : IDisposable
    {
        public MySqlConnection Connection;

        public DBConnection(string connectionString)
        {
            Connection = new MySqlConnection(connectionString);
            this.Connection.Open();
        }
        public void Dispose()
        {
            Connection.Close();
        }
    }
}
