using System;
using System.Data;
using TaskWorkerApp.Services;

namespace TaskWorkerApp.Services
{
    public class ClientService
    {
        private readonly DatabaseService _db;
        public ClientService(DatabaseService db) { _db = db; }

        public void SaveProfile(string name, string phone, string email, string address, string payment)
        {
            // Prevent saving if name, phone, or email is empty
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Name, phone, and email are required.");

            string query = "INSERT INTO Clients (Name, Phone, Email, Address, PaymentInfo) VALUES (@Name, @Phone, @Email, @Address, @Payment)";
            _db.ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Address", address);
                cmd.Parameters.AddWithValue("@Payment", payment);
            });
        }

        public DataTable GetAllClients()
        {
            return _db.ExecuteQuery("SELECT Id, Name FROM Clients ORDER BY Name");
        }

        public ClientProfile GetClientProfile(int clientId)
        {
            var dt = _db.ExecuteQuery("SELECT Name, Phone, Email, Address, PaymentInfo FROM Clients WHERE Id = " + clientId);
            string name = dt.Rows.Count > 0 ? dt.Rows[0]["Name"].ToString() ?? "" : "";
            string phone = dt.Rows.Count > 0 ? dt.Rows[0]["Phone"].ToString() ?? "" : "";
            string email = dt.Rows.Count > 0 ? dt.Rows[0]["Email"].ToString() ?? "" : "";
            string address = dt.Rows.Count > 0 ? dt.Rows[0]["Address"].ToString() ?? "" : "";
            string payment = dt.Rows.Count > 0 ? dt.Rows[0]["PaymentInfo"].ToString() ?? "" : "";
            return new ClientProfile
            {
                Name = name,
                Phone = phone,
                Email = email,
                Address = address,
                PaymentInfo = payment
            };
        }

        public class ClientProfile
        {
            public string Name { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string Address { get; set; } = "";
            public string PaymentInfo { get; set; } = "";
        }
    }
}
