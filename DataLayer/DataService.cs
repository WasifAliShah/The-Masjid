using System.Data;
using Microsoft.Data.SqlClient;

namespace Project2.DataLayer
{
    public class DataService
    {
        private string _connectionString;

        public void DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
        {
            DataTable result = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(result);
                    }
                }
            }

            return result;
        }
    }
}
