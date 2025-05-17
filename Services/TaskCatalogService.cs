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

        public int CreateTaskRequest(int taskId, int clientId, string requestAddress, DateTime preferredTime, int locationId, out string? assignedWorkerName)
        {
            assignedWorkerName = null;
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
                             VALUES (@ClientID, @TaskID, @RequestedDateTime, @PreferredTimeSlot, @RequestAddress, @LocationID, 'open');
                             SELECT SCOPE_IDENTITY();";
            int newRequestId = Convert.ToInt32(_db.ExecuteQueryScalar(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@ClientID", clientId);
                cmd.Parameters.AddWithValue("@TaskID", taskId);
                cmd.Parameters.AddWithValue("@RequestedDateTime", DateTime.Now);
                cmd.Parameters.AddWithValue("@PreferredTimeSlot", preferredTime);
                cmd.Parameters.AddWithValue("@RequestAddress", requestAddress);
                cmd.Parameters.AddWithValue("@LocationID", locationId);
            }));
            // Try to assign a worker immediately
            if (AssignWorkerToTaskRequest(newRequestId))
            {
                // Get the assigned worker's name
                var dt = _db.ExecuteQuery(@"SELECT w.Name FROM TaskAssignments ta JOIN Workers w ON ta.WorkerID = w.Id WHERE ta.RequestID = @RequestId", cmd =>
                {
                    cmd.Parameters.AddWithValue("@RequestId", newRequestId);
                });
                if (dt.Rows.Count > 0)
                    assignedWorkerName = dt.Rows[0]["Name"].ToString();
            }
            return newRequestId;
        }

        // Assigns a worker to a task request if available and updates the status.
        public bool AssignWorkerToTaskRequest(int taskRequestId)
        {
            // Get the task request details
            var dt = _db.ExecuteQuery(@"SELECT tr.TaskID, tr.LocationID, tr.PreferredTimeSlot, t.SpecialtyID
                                         FROM TaskRequests tr
                                         JOIN Tasks t ON tr.TaskID = t.Id
                                         WHERE tr.id = @RequestId",
                cmd => cmd.Parameters.AddWithValue("@RequestId", taskRequestId));
            if (dt.Rows.Count == 0) return false;
            int taskId = (int)dt.Rows[0]["TaskID"];
            int locationId = (int)dt.Rows[0]["LocationID"];
            DateTime preferredTime = (DateTime)dt.Rows[0]["PreferredTimeSlot"];
            int specialtyId = (int)dt.Rows[0]["SpecialtyID"];

            // Find a worker available for this slot, location, and specialty
            var dtWorker = _db.ExecuteQuery(@"
                SELECT TOP 1 wa.WorkerID
                FROM WorkerAvailability wa
                JOIN TimeSlots ts ON wa.TimeSlotID = ts.Id
                WHERE wa.LocationID = @LocationId
                  AND wa.SpecialtyID = @SpecialtyId
                  AND ts.StartTime = @StartTime
                  AND ts.DayOfWeek = @DayOfWeek
                  AND NOT EXISTS (
                      SELECT 1 FROM TaskAssignments ta
                      JOIN TaskRequests tr2 ON ta.RequestID = tr2.id
                      WHERE ta.WorkerID = wa.WorkerID
                        AND tr2.PreferredTimeSlot = @PreferredTime
                        AND ta.Status IN ('scheduled', 'in_progress')
                  )",
                cmd => {
                    cmd.Parameters.AddWithValue("@LocationId", locationId);
                    cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);
                    cmd.Parameters.AddWithValue("@StartTime", preferredTime.TimeOfDay);
                    cmd.Parameters.AddWithValue("@DayOfWeek", (int)preferredTime.DayOfWeek == 0 ? 7 : (int)preferredTime.DayOfWeek);
                    cmd.Parameters.AddWithValue("@PreferredTime", preferredTime);
                });
            if (dtWorker.Rows.Count == 0) return false; // No available worker
            int workerId = (int)dtWorker.Rows[0]["WorkerID"];

            // Assign the worker
            _db.ExecuteNonQuery(@"INSERT INTO TaskAssignments (RequestID, WorkerID, Status) VALUES (@RequestId, @WorkerId, 'scheduled')",
                cmd => {
                    cmd.Parameters.AddWithValue("@RequestId", taskRequestId);
                    cmd.Parameters.AddWithValue("@WorkerId", workerId);
                });

            // Update the task request status
            _db.ExecuteNonQuery("UPDATE TaskRequests SET Status = 'scheduled' WHERE id = @RequestId",
                cmd => cmd.Parameters.AddWithValue("@RequestId", taskRequestId));
            return true;
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
