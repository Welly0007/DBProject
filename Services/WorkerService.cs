using System;
using System.Collections.Generic;
using System.Data;

namespace TaskWorkerApp.Services
{
    public class WorkerService
    {
        private readonly DatabaseService _db;

        public WorkerService(DatabaseService db)
        {
            _db = db;
        }

        public DataTable GetAllWorkers()
        {
            return _db.ExecuteQuery("SELECT Id, Name FROM Workers ORDER BY Name");
        }

        public DataTable GetAllSpecialties()
        {
            return _db.ExecuteQuery("SELECT Id, Name FROM Specialties ORDER BY Name");
        }

        public DataTable GetAllLocations()
        {
            return _db.ExecuteQuery("SELECT Id, Area FROM Locations ORDER BY Area");
        }

        public DataTable GetAllTimeSlots()
        {
            return _db.ExecuteQuery("SELECT Id, DayOfWeek, StartTime, EndTime FROM TimeSlots ORDER BY DayOfWeek, StartTime");
        }

        public WorkerProfile GetWorkerProfile(int workerId)
        {
            var dt = _db.ExecuteQuery("SELECT Name, Phone, Email FROM Workers WHERE Id = " + workerId);
            string name = dt.Rows.Count > 0 ? dt.Rows[0]["Name"].ToString() ?? "" : "";
            string phone = dt.Rows.Count > 0 ? dt.Rows[0]["Phone"].ToString() ?? "" : "";
            string email = dt.Rows.Count > 0 ? dt.Rows[0]["Email"].ToString() ?? "" : "";

            var specialtyIds = new List<int>();
            var dtSpec = _db.ExecuteQuery($"SELECT SpecialtyID FROM WorkerSpecialty WHERE WorkerID = {workerId}");
            foreach (DataRow row in dtSpec.Rows)
                specialtyIds.Add(Convert.ToInt32(row["SpecialtyID"]));

            var locationIds = new List<int>();
            var timeSlotIds = new List<int>();
            var dtAvail = _db.ExecuteQuery($"SELECT DISTINCT LocationID, TimeSlotID FROM WorkerAvailability WHERE WorkerID = {workerId}");
            foreach (DataRow row in dtAvail.Rows)
            {
                int locId = Convert.ToInt32(row["LocationID"]);
                int tsId = Convert.ToInt32(row["TimeSlotID"]);
                if (!locationIds.Contains(locId)) locationIds.Add(locId);
                if (!timeSlotIds.Contains(tsId)) timeSlotIds.Add(tsId);
            }

            return new WorkerProfile
            {
                Name = name,
                Phone = phone,
                Email = email,
                SpecialtyIds = specialtyIds,
                LocationIds = locationIds,
                TimeSlotIds = timeSlotIds
            };
        }

        public void SaveOrUpdateProfile(int? workerId, string name, string phone, string email, List<int> specialtyIds, List<int> locationIds, List<int> timeSlotIds)
        {
            if (workerId == null)
            {
                string insertWorker = "INSERT INTO Workers (Name, Phone, Email) VALUES (@Name, @Phone, @Email); SELECT SCOPE_IDENTITY();";
                int newWorkerId = Convert.ToInt32(_db.ExecuteQueryScalar(insertWorker, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Email", email);
                }));
                workerId = newWorkerId;
            }
            else
            {
                string updateWorker = "UPDATE Workers SET Name = @Name, Phone = @Phone, Email = @Email WHERE Id = @Id";
                _db.ExecuteNonQuery(updateWorker, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Id", workerId.Value);
                });
            }

            _db.ExecuteNonQuery("DELETE FROM WorkerSpecialty WHERE WorkerID = @W", cmd => cmd.Parameters.AddWithValue("@W", workerId.Value));
            foreach (var sid in specialtyIds)
            {
                _db.ExecuteNonQuery("INSERT INTO WorkerSpecialty (WorkerID, SpecialtyID) VALUES (@W, @S)", cmd =>
                {
                    cmd.Parameters.AddWithValue("@W", workerId.Value);
                    cmd.Parameters.AddWithValue("@S", sid);
                });
            }

            _db.ExecuteNonQuery("DELETE FROM WorkerAvailability WHERE WorkerID = @W", cmd => cmd.Parameters.AddWithValue("@W", workerId.Value));
            if (specialtyIds.Count > 0 && locationIds.Count > 0 && timeSlotIds.Count > 0)
            {
                foreach (var locId in locationIds)
                {
                    foreach (var tsId in timeSlotIds)
                    {
                        var sid = specialtyIds.First();
                        _db.ExecuteNonQuery(
                            "INSERT INTO WorkerAvailability (WorkerID, TimeSlotID, LocationID, SpecialtyID) VALUES (@W, @T, @L, @S)",
                            cmd =>
                            {
                                cmd.Parameters.AddWithValue("@W", workerId.Value);
                                cmd.Parameters.AddWithValue("@T", tsId);
                                cmd.Parameters.AddWithValue("@L", locId);
                                cmd.Parameters.AddWithValue("@S", sid);
                            });
                    }
                }
            }
        }

        public List<int> GetAvailableWorkersForTask(int taskId, int locationId, DateTime preferredDateTime)
        {
            // Get the specialty required for the task
            var dtTask = _db.ExecuteQuery("SELECT SpecialtyID FROM Tasks WHERE Id = @TaskId", cmd =>
            {
                cmd.Parameters.AddWithValue("@TaskId", taskId);
            });
            if (dtTask.Rows.Count == 0) return new List<int>();
            int specialtyId = Convert.ToInt32(dtTask.Rows[0]["SpecialtyID"]);

            // Find the matching time slot
            int dayOfWeek = (int)preferredDateTime.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // SQL: 1=Monday, C#: 0=Sunday
            TimeSpan time = preferredDateTime.TimeOfDay;
            var dtSlot = _db.ExecuteQuery("SELECT Id FROM TimeSlots WHERE DayOfWeek = @Day AND StartTime <= @Time AND EndTime > @Time", cmd =>
            {
                cmd.Parameters.AddWithValue("@Day", dayOfWeek);
                cmd.Parameters.AddWithValue("@Time", time);
            });
            if (dtSlot.Rows.Count == 0) return new List<int>();
            int timeSlotId = Convert.ToInt32(dtSlot.Rows[0]["Id"]);

            // Find workers available for this slot, location, and specialty
            var dtWorkers = _db.ExecuteQuery(@"
                SELECT wa.WorkerID
                FROM WorkerAvailability wa
                WHERE wa.TimeSlotID = @TimeSlotId
                  AND wa.LocationID = @LocationId
                  AND wa.SpecialtyID = @SpecialtyId
                  AND NOT EXISTS (
                      SELECT 1 FROM TaskAssignments ta
                      JOIN TaskRequests tr ON ta.RequestID = tr.id
                      WHERE ta.WorkerID = wa.WorkerID
                        AND tr.LocationID = @LocationId
                        AND tr.RequestedDateTime = @PrefDate
                        AND ta.Status IN ('scheduled', 'in_progress')
                  )
                GROUP BY wa.WorkerID
            ", cmd =>
            {
                cmd.Parameters.AddWithValue("@TimeSlotId", timeSlotId);
                cmd.Parameters.AddWithValue("@LocationId", locationId);
                cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);
                cmd.Parameters.AddWithValue("@PrefDate", preferredDateTime);
            });
            var result = new List<int>();
            foreach (DataRow row in dtWorkers.Rows)
                result.Add(Convert.ToInt32(row["WorkerID"]));
            return result;
        }

        /// <summary>
        /// Returns a list of time slot IDs that have at least one available worker for the given task and location.
        /// </summary>
        public List<int> GetAvailableTimeSlotsForTask(int taskId, int locationId)
        {
            // Get the specialty required for the task
            var dtTask = _db.ExecuteQuery("SELECT SpecialtyID FROM Tasks WHERE Id = @TaskId", cmd =>
            {
                cmd.Parameters.AddWithValue("@TaskId", taskId);
            });
            if (dtTask.Rows.Count == 0) return new List<int>();
            int specialtyId = Convert.ToInt32(dtTask.Rows[0]["SpecialtyID"]);

            // Find all time slots with at least one available worker for this task/location
            var dtSlots = _db.ExecuteQuery(@"
                SELECT DISTINCT wa.TimeSlotID
                FROM WorkerAvailability wa
                WHERE wa.LocationID = @LocationId
                  AND wa.SpecialtyID = @SpecialtyId
            ", cmd =>
            {
                cmd.Parameters.AddWithValue("@LocationId", locationId);
                cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);
            });
            var result = new List<int>();
            foreach (DataRow row in dtSlots.Rows)
                result.Add(Convert.ToInt32(row["TimeSlotID"]));
            return result;
        }

        public class WorkerProfile
        {
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public List<int> SpecialtyIds { get; set; } = new List<int>();
            public List<int> LocationIds { get; set; } = new List<int>();
            public List<int> TimeSlotIds { get; set; } = new List<int>();
        }
    }
}
