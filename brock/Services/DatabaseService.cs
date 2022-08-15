using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.Services
{
    public class DatabaseService
    {
        private readonly ConfigService _config;
        private SqlConnection _connection;
        private const string LP = "[DatabaseService]";  // Log prefix


        public DatabaseService(ConfigService config = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException($"{LP} Constructor - config arg is null?");
            }
            _config = config;
        }

        public void Initialize()
        {
            _connection = new SqlConnection(_config.Get<string>("DbConnectionString"));
        }
        public string GetLatestTestValue()
        {

            return "NOT IMPLEMENTED";
        }

        public List<string> GetAllTestRows()
        {
            List<string> testRows = new List<string>();
            List<List<object>> rawRows = DoQuery("SELECT * FROM brock.dbo.Test");
            foreach (List<object> rawRow in rawRows) {
                testRows.Add($"CreatedDate={(DateTime)rawRow[0]}, TextValue={(string)rawRow[1]}");
            }
            return testRows;
        }

        private List<List<object>> DoQuery(string query)
        {
            List<List<object>> rows = new List<List<object>>();

            SqlCommand cmd = new SqlCommand(query, _connection);
            cmd.Connection.Open();
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<object> row = new List<object>();
                    for(int i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add(reader.GetValue(i));
                    }
                    rows.Add(row);
                }
            }
            return rows;
        }
    }
}
