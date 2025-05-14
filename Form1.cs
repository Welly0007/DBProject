using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace TaskWorkerApp
{
    public partial class Form1 : Form
    {
        // Connection string to the SQL Server database
        private readonly string _connectionString = 
            "Server=WELLY-PC\\SQLEXPRESS;Database=TaskWorkerDB;Trusted_Connection=True;TrustServerCertificate=True;";
        
        // UI Controls (nullable to avoid initialization warnings)
        private TextBox? txtWorkerName;
        private Button? btnAddWorker;  
        private Button? btnUpdateWorker;
        private Button? btnDeleteWorker;
        private DataGridView? dgvWorkers;
        private Label? lblStatus;

        public Form1()
        {
            InitializeComponent();
            SetupUI(); // Dynamically sets up the UI controls
            EnsureDatabaseCreated(); // Ensures the database and table exist
        }

        private void SetupUI()
        {
            // Configure form properties
            this.Text = "Worker Management";
            this.Size = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create and configure UI controls
            Label lblName = new Label
            {
                Text = "Worker Name:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 20)
            };

            txtWorkerName = new TextBox
            {
                Location = new System.Drawing.Point(120, 20),
                Size = new System.Drawing.Size(200, 20)
            };

            btnAddWorker = new Button
            {
                Text = "Add Worker",
                Location = new System.Drawing.Point(330, 20),
                Size = new System.Drawing.Size(80, 25)
            };
            btnAddWorker.Click += BtnAddWorker_Click;

            btnUpdateWorker = new Button
            {
                Text = "Update",
                Location = new System.Drawing.Point(415, 20),
                Size = new System.Drawing.Size(80, 25),
                Enabled = false // Initially disabled until a worker is selected
            };
            btnUpdateWorker.Click += BtnUpdateWorker_Click;

            btnDeleteWorker = new Button
            {
                Text = "Delete",
                Location = new System.Drawing.Point(500, 20),
                Size = new System.Drawing.Size(80, 25),
                Enabled = false // Initially disabled until a worker is selected
            };
            btnDeleteWorker.Click += BtnDeleteWorker_Click;

            dgvWorkers = new DataGridView
            {
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(560, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvWorkers.SelectionChanged += DgvWorkers_SelectionChanged;

            lblStatus = new Label
            {
                Text = "Ready",
                Location = new System.Drawing.Point(20, 320),
                Size = new System.Drawing.Size(560, 20)
            };

            // Add controls to the form
            this.Controls.Add(lblName);
            this.Controls.Add(txtWorkerName);
            this.Controls.Add(btnAddWorker);
            this.Controls.Add(btnUpdateWorker);
            this.Controls.Add(btnDeleteWorker);
            this.Controls.Add(dgvWorkers);
            this.Controls.Add(lblStatus);

            // Set form event handlers
            this.Load += Form1_Load;
        }

        private void EnsureDatabaseCreated()
        {
            // Creates the Workers table if it doesn't already exist
            try
            {
                string createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Workers]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[Workers] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Name] NVARCHAR(100) NOT NULL
                        )
                    END";

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    using (SqlCommand command = new SqlCommand(createTableQuery, connection))
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        lblStatus.Text = "Database initialized successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // Load workers into the DataGridView when the form loads
            LoadWorkers();
        }

        private void BtnAddWorker_Click(object? sender, EventArgs e)
        {
            // Adds a new worker to the database
            if (txtWorkerName == null || string.IsNullOrWhiteSpace(txtWorkerName.Text))
            {
                lblStatus!.Text = "Error: Worker name cannot be empty";
                return;
            }

            try
            {
                string query = "INSERT INTO Workers (Name) VALUES (@Name)";
                
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", txtWorkerName.Text);
                        
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        lblStatus!.Text = $"Added worker: {txtWorkerName.Text} - {rowsAffected} row(s) affected";
                    }
                }
                
                txtWorkerName.Clear();
                LoadWorkers();
            }
            catch (Exception ex)
            {
                lblStatus!.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to add worker: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateWorker_Click(object? sender, EventArgs e)
        {
            // Updates the selected worker's name in the database
            if (dgvWorkers == null || dgvWorkers.SelectedRows.Count == 0)
            {
                lblStatus!.Text = "Error: No worker selected";
                return;
            }

            if (txtWorkerName == null || string.IsNullOrWhiteSpace(txtWorkerName.Text))
            {
                lblStatus!.Text = "Error: Worker name cannot be empty";
                return;
            }

            try
            {
                int workerId = Convert.ToInt32(dgvWorkers.SelectedRows[0].Cells["Id"].Value);
                
                string query = "UPDATE Workers SET Name = @Name WHERE Id = @Id";
                
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", txtWorkerName.Text);
                        command.Parameters.AddWithValue("@Id", workerId);
                        
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        lblStatus!.Text = $"Updated worker: {txtWorkerName.Text} - {rowsAffected} row(s) affected";
                    }
                }
                
                txtWorkerName.Clear();
                btnUpdateWorker!.Enabled = false;
                btnDeleteWorker!.Enabled = false;
                LoadWorkers();
            }
            catch (Exception ex)
            {
                lblStatus!.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to update worker: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteWorker_Click(object? sender, EventArgs e)
        {
            // Deletes the selected worker from the database
            if (dgvWorkers == null || dgvWorkers.SelectedRows.Count == 0)
            {
                lblStatus!.Text = "Error: No worker selected";
                return;
            }

            try
            {
                int workerId = Convert.ToInt32(dgvWorkers.SelectedRows[0].Cells["Id"].Value);
                string workerName = dgvWorkers.SelectedRows[0].Cells["Name"].Value?.ToString() ?? string.Empty;

                DialogResult result = MessageBox.Show($"Are you sure you want to delete worker '{workerName}'?", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    string query = "DELETE FROM Workers WHERE Id = @Id";
                    
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", workerId);
                            
                            connection.Open();
                            int rowsAffected = command.ExecuteNonQuery();
                            lblStatus!.Text = $"Deleted worker: {workerName} - {rowsAffected} row(s) affected";
                        }
                    }
                    
                    txtWorkerName.Clear();
                    btnUpdateWorker!.Enabled = false;
                    btnDeleteWorker!.Enabled = false;
                    LoadWorkers();
                }
            }
            catch (Exception ex)
            {
                lblStatus!.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to delete worker: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvWorkers_SelectionChanged(object? sender, EventArgs e)
        {
            // Updates the UI when a worker is selected in the DataGridView
            if (dgvWorkers != null && dgvWorkers.SelectedRows.Count > 0)
            {
                string workerName = dgvWorkers.SelectedRows[0].Cells["Name"].Value?.ToString() ?? string.Empty;
                txtWorkerName!.Text = workerName; // Null-forgiving operator
                btnUpdateWorker!.Enabled = true; // Null-forgiving operator
                btnDeleteWorker!.Enabled = true; // Null-forgiving operator
            }
            else
            {
                txtWorkerName?.Clear();
                if (btnUpdateWorker != null) btnUpdateWorker.Enabled = false;
                if (btnDeleteWorker != null) btnDeleteWorker.Enabled = false;
            }
        }

        private void LoadWorkers()
        {
            // Loads all workers from the database into the DataGridView
            if (dgvWorkers == null || lblStatus == null) return;

            try
            {
                string query = "SELECT Id, Name FROM Workers ORDER BY Name";
                
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    
                    dgvWorkers!.DataSource = dataTable; // Null-forgiving operator
                    lblStatus!.Text = $"Loaded {dataTable.Rows.Count} workers"; // Null-forgiving operator
                }
            }
            catch (Exception ex)
            {
                lblStatus!.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to load workers: {ex.Message}", 
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}