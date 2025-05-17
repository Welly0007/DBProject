using System.Data;

namespace TaskWorkerApp.Services
{
    public class TaskCatalogService
    {
        private readonly DatabaseService _db;
        public TaskCatalogService(DatabaseService db) { _db = db; }

        public DataTable SearchTasks(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                string allQuery = @"SELECT t.TaskName, t.AverageDuration, t.AverageFee, s.Name AS Specialty
                                    FROM Tasks t
                                    LEFT JOIN Specialties s ON t.SpecialtyID = s.Id
                                    ORDER BY t.TaskName";
                return _db.ExecuteQuery(allQuery);
            }
            string query = @"SELECT id, TaskName, AverageDuration, AverageFee 
                             FROM Tasks 
                             WHERE TaskName LIKE @SearchTerm 
                             ORDER BY TaskName";
            DataTable result = _db.ExecuteQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@SearchTerm", $"%{search}%");
            });
            
            // Ensure the column name is correctly defined
            if (result.Columns.Contains("id") && !result.Columns.Contains("Id"))
            {
                result.Columns["id"].ColumnName = "Id";
            }
            
            return result;
        }

        public void CreateTaskRequest(int taskId, int clientId, string requestAddress, DateTime preferredTime, int locationId)
        {
            // Ensure the TaskID exists in the Tasks table
            string taskCheckQuery = "SELECT COUNT(1) FROM Tasks WHERE Id = @TaskID";
            int taskExists = Convert.ToInt32(_db.ExecuteQueryScalar(taskCheckQuery, cmd =>
            {
                cmd.Parameters.AddWithValue("@TaskID", taskId);
            }));

            if (taskExists == 0)
            {
                throw new Exception("The specified task does not exist.");
            }

            // Ensure the ClientID exists in the Clients table
            string clientCheckQuery = "SELECT COUNT(1) FROM Clients WHERE Id = @ClientID";
            int clientExists = Convert.ToInt32(_db.ExecuteQueryScalar(clientCheckQuery, cmd =>
            {
                cmd.Parameters.AddWithValue("@ClientID", clientId);
            }));

            if (clientExists == 0)
            {
                throw new Exception("The specified client does not exist.");
            }

            // Ensure the LocationID exists in the Locations table
            string locationCheckQuery = "SELECT COUNT(1) FROM Locations WHERE Id = @LocationID";
            int locationExists = Convert.ToInt32(_db.ExecuteQueryScalar(locationCheckQuery, cmd =>
            {
                cmd.Parameters.AddWithValue("@LocationID", locationId);
            }));

            if (locationExists == 0)
            {
                throw new Exception("The specified location does not exist.");
            }

            string query = @"INSERT INTO TaskRequests (ClientID, TaskID, RequestedDateTime, PreferredTimeSlot, RequestAddress, LocationID, Status)
                             VALUES (@ClientID, @TaskID, @RequestedDateTime, @PreferredTimeSlot, @RequestAddress, @LocationID, 'open')";
            _db.ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@ClientID", clientId);
                cmd.Parameters.AddWithValue("@TaskID", taskId);
                cmd.Parameters.AddWithValue("@RequestedDateTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@PreferredTimeSlot", preferredTime);
                cmd.Parameters.AddWithValue("@RequestAddress", requestAddress);
                cmd.Parameters.AddWithValue("@LocationID", locationId);
            });
        }

        public DataTable GetAllTasks()
        {
            string query = @"SELECT t.id, t.TaskName, t.AverageDuration, t.AverageFee, s.Name AS Specialty
                             FROM Tasks t
                             LEFT JOIN Specialties s ON t.SpecialtyID = s.Id
                             ORDER BY t.TaskName";
            return _db.ExecuteQuery(query);
        }
    }
}
