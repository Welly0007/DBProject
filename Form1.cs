using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using TaskWorkerApp.Services;

namespace TaskWorkerApp
{
    public partial class Form1 : Form
    {
        private readonly DatabaseService _databaseService;
        private readonly WorkerService _workerService;
        private readonly ClientService _clientService;
        private readonly TaskCatalogService _taskCatalogService;

        private TabControl? tabControl;

        // Registration/Profile controls
        private Panel? pnlMainMenu;
        private Button? btnClient;
        private Button? btnWorker;
        private TabPage? tabClientProfile;
        private TabPage? tabWorkerProfile;
        private TabPage? tabTaskCatalog;

        private int? _loggedInClientId; // Track the logged-in client ID
        private int? _loggedInWorkerId; // Track the logged-in worker ID

        public Form1()
        {
            InitializeComponent();
            string schemaFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheme", "initial-schema.sql");
            _databaseService = new DatabaseService(
                "Server=ALYY;Database=TaskWorkerDB;Trusted_Connection=True;TrustServerCertificate=True;",
                schemaFilePath);
            _workerService = new WorkerService(_databaseService);
            _clientService = new ClientService(_databaseService);
            _taskCatalogService = new TaskCatalogService(_databaseService);

            EnsureDatabaseCreated();
            SetupMainMenu();
        }

        private void SetupMainMenu()
        {
            this.Text = "Task Worker App";
            this.Size = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            pnlMainMenu = new Panel { Dock = DockStyle.Fill };
            btnClient = new Button { Text = "Client", Location = new System.Drawing.Point(200, 200), Size = new System.Drawing.Size(200, 60) };
            btnWorker = new Button { Text = "Worker", Location = new System.Drawing.Point(500, 200), Size = new System.Drawing.Size(200, 60) };

            // Add Reset Database button
            Button btnResetDb = new Button
            {
                Text = "Reset Database",
                Location = new System.Drawing.Point(350, 300),
                Size = new System.Drawing.Size(200, 40),
                BackColor = System.Drawing.Color.LightCoral
            };

            btnResetDb.Click += (s, e) =>
            {
                if (MessageBox.Show(
                    "This will reset the database to its initial state. All data will be lost.\n\nAre you sure you want to continue?",
                    "Reset Database",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        _databaseService.ResetDatabase();
                        MessageBox.Show("Database reset successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error resetting database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            btnClient.Click += (s, e) => ShowTabs("client");
            btnWorker.Click += (s, e) => ShowTabs("worker");

            pnlMainMenu.Controls.Add(btnClient);
            pnlMainMenu.Controls.Add(btnWorker);
            pnlMainMenu.Controls.Add(btnResetDb);

            this.Controls.Add(pnlMainMenu);
        }

        private void ShowTabs(string userType)
        {
            this.Controls.Remove(pnlMainMenu);
            tabControl = new TabControl { Dock = DockStyle.Fill };

            if (userType == "client")
            {
                tabClientProfile = new TabPage("Client Profile");
                SetupClientProfileTab(tabClientProfile);
                tabControl.TabPages.Add(tabClientProfile);

                // Only add the Task Catalog tab if a client is logged in
                if (_loggedInClientId != null)
                {
                    tabTaskCatalog = new TabPage("Task Catalog");
                    SetupTaskCatalogTab(tabTaskCatalog);
                    tabControl.TabPages.Add(tabTaskCatalog);

                    var tabClientRequests = new TabPage("My Requests");
                    SetupClientRequestsTab(tabClientRequests);
                    tabControl.TabPages.Add(tabClientRequests);
                }
            }
            else // worker
            {
                if (_loggedInWorkerId == null)
                {
                    var tabWorkerLogin = new TabPage("Worker Login/Signup");
                    SetupWorkerLoginTab(tabWorkerLogin);
                    tabControl.TabPages.Add(tabWorkerLogin);
                }
                else
                {
                    var tabAssigned = new TabPage("Assigned Tasks");
                    SetupWorkerAssignedTasksTab(tabAssigned);
                    tabControl.TabPages.Add(tabAssigned);

                    tabWorkerProfile = new TabPage("Edit Profile");
                    SetupWorkerProfileTab(tabWorkerProfile);
                    tabControl.TabPages.Add(tabWorkerProfile);
                }
            }

            this.Controls.Add(tabControl);
        }

        private void AddBackToMenuButton(TabPage tab)
        {
            Button btnBack = new Button { Text = "Back to Main Menu", Location = new System.Drawing.Point(10, 10), Width = 150 };
            Button btnSignOut = new Button { Text = "Sign Out", Location = new System.Drawing.Point(170, 10), Width = 100 };

            btnBack.Click += (s, e) =>
            {
                this.Controls.Remove(tabControl!);
                tabControl = null; // Reset tabControl to ensure proper reinitialization
                this.Controls.Add(pnlMainMenu!);
            };

            btnSignOut.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to sign out?", "Sign Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Reset login state
                    _loggedInClientId = null;
                    _loggedInWorkerId = null;

                    this.Controls.Remove(tabControl!);
                    tabControl = null;
                    this.Controls.Add(pnlMainMenu!);

                    MessageBox.Show("You have been signed out successfully.", "Sign Out", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            tab.Controls.Add(btnBack);
            tab.Controls.Add(btnSignOut);
        }

        private void SetupClientProfileTab(TabPage tab)
        {
            AddBackToMenuButton(tab);
            int yOffset = 50;

            // Login/Create buttons
            Button btnLogin = new Button { Text = "Login", Location = new System.Drawing.Point(150, 30 + yOffset), Width = 120 };
            Button btnCreate = new Button { Text = "Create Profile", Location = new System.Drawing.Point(300, 30 + yOffset), Width = 120 };

            // Login controls
            Label lblSelect = new Label { Text = "Select Client:", Location = new System.Drawing.Point(30, 80 + yOffset) };
            ComboBox cmbClients = new ComboBox { Location = new System.Drawing.Point(150, 80 + yOffset), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnDoLogin = new Button { Text = "Login", Location = new System.Drawing.Point(370, 80 + yOffset), Width = 100 };

            // Profile fields for creation
            Label lblName = new Label { Text = "Name:", Location = new System.Drawing.Point(30, 140 + yOffset) };
            TextBox txtName = new TextBox { Location = new System.Drawing.Point(170, 140 + yOffset), Width = 200 };
            Label lblPhone = new Label { Text = "Phone:", Location = new System.Drawing.Point(30, 180 + yOffset) };
            TextBox txtPhone = new TextBox { Location = new System.Drawing.Point(170, 180 + yOffset), Width = 200 };
            Label lblEmail = new Label { Text = "Email:", Location = new System.Drawing.Point(30, 220 + yOffset) };
            TextBox txtEmail = new TextBox { Location = new System.Drawing.Point(170, 220 + yOffset), Width = 200 };
            Label lblAddress = new Label { Text = "Address:", Location = new System.Drawing.Point(30, 260 + yOffset) };
            TextBox txtAddress = new TextBox { Location = new System.Drawing.Point(170, 260 + yOffset), Width = 200 };
            Label lblPayment = new Label { Text = "Payment Info:", Location = new System.Drawing.Point(30, 300 + yOffset) };
            TextBox txtPayment = new TextBox { Location = new System.Drawing.Point(170, 300 + yOffset), Width = 200 };
            Button btnSubmit = new Button { Text = "Submit", Location = new System.Drawing.Point(170, 340 + yOffset) };

            // Helper to show/hide login/create UI
            void ShowLoginUI()
            {
                cmbClients.Items.Clear();
                foreach (DataRow row in _clientService.GetAllClients().Rows)
                    cmbClients.Items.Add(new ComboBoxItem(row["Name"]?.ToString() ?? "", (int)row["Id"]));

                lblSelect.Visible = true;
                cmbClients.Visible = true;
                btnDoLogin.Visible = true;

                lblName.Visible = false; txtName.Visible = false;
                lblPhone.Visible = false; txtPhone.Visible = false;
                lblEmail.Visible = false; txtEmail.Visible = false;
                lblAddress.Visible = false; txtAddress.Visible = false;
                lblPayment.Visible = false; txtPayment.Visible = false;
                btnSubmit.Visible = false;
            }
            void ShowCreateUI()
            {
                lblSelect.Visible = false;
                cmbClients.Visible = false;
                btnDoLogin.Visible = false;

                lblName.Visible = true; txtName.Visible = true;
                lblPhone.Visible = true; txtPhone.Visible = true;
                lblEmail.Visible = true; txtEmail.Visible = true;
                lblAddress.Visible = true; txtAddress.Visible = true;
                lblPayment.Visible = true; txtPayment.Visible = true;
                btnSubmit.Visible = true;

                txtName.Text = "";
                txtPhone.Text = "";
                txtEmail.Text = "";
                txtAddress.Text = "";
                txtPayment.Text = "";
            }

            // Initial state: show only login/create buttons
            btnLogin.Click += (s, e) => ShowLoginUI();
            btnCreate.Click += (s, e) => ShowCreateUI();

            btnDoLogin.Click += (s, e) =>
            {
                if (cmbClients.SelectedItem is ComboBoxItem clientItem)
                {
                    var profile = _clientService.GetClientProfile(clientItem.Value);
                    txtName.Text = profile.Name;
                    txtPhone.Text = profile.Phone;
                    txtEmail.Text = profile.Email;
                    txtAddress.Text = profile.Address;
                    txtPayment.Text = profile.PaymentInfo;

                    _loggedInClientId = clientItem.Value; // Set the logged-in client ID
                    MessageBox.Show("Logged in as " + profile.Name);

                    // Refresh the tabs to show the Task Catalog
                    this.Controls.Remove(tabControl!);
                    ShowTabs("client");
                }
            };

            btnSubmit.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) ||
                    string.IsNullOrWhiteSpace(txtPhone.Text) ||
                    string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Name, phone, and email are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _clientService.SaveProfile(txtName.Text, txtPhone.Text, txtEmail.Text, txtAddress.Text, txtPayment.Text);
                MessageBox.Show("Profile created. Please login.");
                ShowLoginUI();
            };

            // Add controls to tab
            tab.Controls.Add(btnLogin);
            tab.Controls.Add(btnCreate);
            tab.Controls.Add(lblSelect); tab.Controls.Add(cmbClients); tab.Controls.Add(btnDoLogin);
            tab.Controls.Add(lblName); tab.Controls.Add(txtName);
            tab.Controls.Add(lblPhone); tab.Controls.Add(txtPhone);
            tab.Controls.Add(lblEmail); tab.Controls.Add(txtEmail);
            tab.Controls.Add(lblAddress); tab.Controls.Add(txtAddress);
            tab.Controls.Add(lblPayment); tab.Controls.Add(txtPayment);
            tab.Controls.Add(btnSubmit);

            // Hide all except login/create buttons at start
            lblSelect.Visible = false; cmbClients.Visible = false; btnDoLogin.Visible = false;
            lblName.Visible = false; txtName.Visible = false;
            lblPhone.Visible = false; txtPhone.Visible = false;
            lblEmail.Visible = false; txtEmail.Visible = false;
            lblAddress.Visible = false; txtAddress.Visible = false;
            lblPayment.Visible = false; txtPayment.Visible = false;
            btnSubmit.Visible = false;
        }

        private void SetupWorkerProfileTab(TabPage tab)
        {
            AddBackToMenuButton(tab);
            int yOffset = 50;

            // Profile fields
            Label lblName = new Label { Text = "Name:", Location = new System.Drawing.Point(30, 80 + yOffset) };
            TextBox txtName = new TextBox { Location = new System.Drawing.Point(170, 80 + yOffset), Width = 200 };
            Label lblPhone = new Label { Text = "Phone:", Location = new System.Drawing.Point(30, 120 + yOffset) };
            TextBox txtPhone = new TextBox { Location = new System.Drawing.Point(170, 120 + yOffset), Width = 200 };
            Label lblEmail = new Label { Text = "Email:", Location = new System.Drawing.Point(30, 160 + yOffset) };
            TextBox txtEmail = new TextBox { Location = new System.Drawing.Point(170, 160 + yOffset), Width = 200 };

            // Specialties
            Label lblSpecialty = new Label { Text = "Specialties:", Location = new System.Drawing.Point(30, 200 + yOffset) };
            CheckedListBox clbSpecialties = new CheckedListBox { Location = new System.Drawing.Point(140, 200 + yOffset), Width = 200, Height = 80 };
            foreach (DataRow row in _workerService.GetAllSpecialties().Rows)
                clbSpecialties.Items.Add(new ComboBoxItem(row["Name"]?.ToString() ?? "", (int)row["Id"]));

            // Locations
            Label lblLocation = new Label { Text = "Locations:", Location = new System.Drawing.Point(30, 300 + yOffset) };
            CheckedListBox clbLocations = new CheckedListBox { Location = new System.Drawing.Point(140, 300 + yOffset), Width = 200, Height = 80 };
            foreach (DataRow row in _workerService.GetAllLocations().Rows)
                clbLocations.Items.Add(new ComboBoxItem(row["Area"]?.ToString() ?? "", (int)row["Id"]));

            // TimeSlots
            Label lblTimeSlots = new Label { Text = "Time Slots:", Location = new System.Drawing.Point(350, 200 + yOffset) };
            CheckedListBox clbTimeSlots = new CheckedListBox { Location = new System.Drawing.Point(460, 200 + yOffset), Width = 300, Height = 180 };
            foreach (DataRow row in _workerService.GetAllTimeSlots().Rows)
            {
                string slotText = $"Day {row["DayOfWeek"]}: {row["StartTime"]}-{row["EndTime"]}";
                clbTimeSlots.Items.Add(new ComboBoxItem(slotText, (int)row["Id"]));
            }

            Button btnSave = new Button { Text = "Save", Location = new System.Drawing.Point(170, 400 + yOffset) };

            // Load the logged-in worker's profile automatically
            if (_loggedInWorkerId != null)
            {
                var profile = _workerService.GetWorkerProfile(_loggedInWorkerId.Value);
                txtName.Text = profile.Name;
                txtPhone.Text = profile.Phone;
                txtEmail.Text = profile.Email;
                txtName.Enabled = true;
                txtPhone.Enabled = true;
                txtEmail.Enabled = true;
                clbSpecialties.Enabled = true;
                clbLocations.Enabled = true;
                clbTimeSlots.Enabled = true;
                btnSave.Enabled = true;
                // Set specialties
                for (int i = 0; i < clbSpecialties.Items.Count; i++)
                {
                    var item = (ComboBoxItem)clbSpecialties.Items[i];
                    clbSpecialties.SetItemChecked(i, profile.SpecialtyIds.Contains(item.Value));
                }
                // Set locations
                for (int i = 0; i < clbLocations.Items.Count; i++)
                {
                    var item = (ComboBoxItem)clbLocations.Items[i];
                    clbLocations.SetItemChecked(i, profile.LocationIds.Contains(item.Value));
                }
                // Set time slots
                for (int i = 0; i < clbTimeSlots.Items.Count; i++)
                {
                    var item = (ComboBoxItem)clbTimeSlots.Items[i];
                    clbTimeSlots.SetItemChecked(i, profile.TimeSlotIds.Contains(item.Value));
                }
            }

            btnSave.Click += (s, e) =>
            {
                var specialtyIds = clbSpecialties.CheckedItems.OfType<ComboBoxItem>().Select(x => x.Value).ToList();
                var locationIds = clbLocations.CheckedItems.OfType<ComboBoxItem>().Select(x => x.Value).ToList();
                var timeSlotIds = clbTimeSlots.CheckedItems.OfType<ComboBoxItem>().Select(x => x.Value).ToList();
                if (_loggedInWorkerId != null)
                {
                    _workerService.SaveOrUpdateProfile(_loggedInWorkerId, txtName.Text, txtPhone.Text, txtEmail.Text, specialtyIds, locationIds, timeSlotIds);
                    MessageBox.Show("Profile updated.");
                }
            };

            tab.Controls.Add(lblName); tab.Controls.Add(txtName);
            tab.Controls.Add(lblPhone); tab.Controls.Add(txtPhone);
            tab.Controls.Add(lblEmail); tab.Controls.Add(txtEmail);
            tab.Controls.Add(lblSpecialty); tab.Controls.Add(clbSpecialties);
            tab.Controls.Add(lblLocation); tab.Controls.Add(clbLocations);
            tab.Controls.Add(lblTimeSlots); tab.Controls.Add(clbTimeSlots);
            tab.Controls.Add(btnSave);
        }

        private void SetupTaskCatalogTab(TabPage tab)
        {
            AddBackToMenuButton(tab);
            int yOffset = 50;

            // Task listing 
            Label lblTaskListing = new Label { Text = "Available Tasks:", Location = new System.Drawing.Point(20, 20 + yOffset), Width = 150, Font = new Font(Font.FontFamily, 10, FontStyle.Bold) };

            // Task info panel
            Label lblTaskInfo = new Label
            {
                Location = new System.Drawing.Point(140, 220 + yOffset),
                Width = 600,
                Height = 100,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            // List view for showing all tasks
            ListView lvTasks = new ListView
            {
                Location = new System.Drawing.Point(20, 50 + yOffset),
                Size = new System.Drawing.Size(800, 150),
                View = View.Details,
                FullRowSelect = true
            };

            // Add columns to the list view
            lvTasks.Columns.Add("Task", 300);
            lvTasks.Columns.Add("Duration (min)", 100);
            lvTasks.Columns.Add("Fee ($)", 100);
            lvTasks.Columns.Add("Specialty", 200);

            // Load tasks into list view
            DataTable tasks = _taskCatalogService.GetAllTasks();
            foreach (DataRow row in tasks.Rows)
            {
                int taskId = Convert.ToInt32(row["id"]);
                string taskName = row["TaskName"].ToString() ?? "";
                string duration = row["AverageDuration"].ToString() ?? "";
                string fee = row["AverageFee"].ToString() ?? "";
                string specialty = row["Specialty"]?.ToString() ?? "";

                // Add to list view
                var item = new ListViewItem(new[] { taskName, duration, fee, specialty }) { Tag = taskId };
                lvTasks.Items.Add(item);
            }

            // Update task info when selection changes
            lvTasks.SelectedIndexChanged += (s, e) =>
            {
                if (lvTasks.SelectedItems.Count > 0)
                {
                    string taskName = lvTasks.SelectedItems[0].SubItems[0].Text;
                    string duration = lvTasks.SelectedItems[0].SubItems[1].Text;
                    string fee = lvTasks.SelectedItems[0].SubItems[2].Text;

                    lblTaskInfo.Text = $"Task: {taskName}\r\n" +
                                      $"Duration: {duration} minutes\r\n" +
                                      $"Fee: ${fee}";
                }
                else
                {
                    lblTaskInfo.Text = "";
                }
            };

            // Request button
            Button btnRequestTask = new Button { Text = "Request Task", Location = new System.Drawing.Point(140, 340 + yOffset), Width = 150 };
            btnRequestTask.Click += (s, e) =>
            {
                if (lvTasks.SelectedItems.Count > 0 && lvTasks.SelectedItems[0].Tag != null)
                {
                    int taskId = Convert.ToInt32(lvTasks.SelectedItems[0].Tag);
                    OpenTaskRequestForm(taskId);
                }
                else
                {
                    MessageBox.Show("Please select a task to request.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // Add controls
            tab.Controls.Add(lblTaskListing);
            tab.Controls.Add(lvTasks);
            tab.Controls.Add(lblTaskInfo);
            tab.Controls.Add(btnRequestTask);
        }

        private void SetupClientRequestsTab(TabPage tab)
        {
            AddBackToMenuButton(tab);
            if (_loggedInClientId == null) return;
            int yOffset = 50;

            Label lblTitle = new Label
            {
                Text = "My Requested Tasks:",
                Location = new System.Drawing.Point(20, 20 + yOffset),
                Width = 250,
                Font = new System.Drawing.Font(Font.FontFamily, 12, System.Drawing.FontStyle.Bold)
            };

            ListView lvRequests = new ListView
            {
                Location = new System.Drawing.Point(20, 50 + yOffset),
                Size = new System.Drawing.Size(800, 300),
                View = View.Details,
                FullRowSelect = true
            };
            lvRequests.Columns.Add("Task", 200);
            lvRequests.Columns.Add("Location", 120);
            lvRequests.Columns.Add("Time Slot", 150);
            lvRequests.Columns.Add("Status", 100);
            lvRequests.Columns.Add("Worker", 150);
            lvRequests.Columns.Add("Requested On", 150);
            lvRequests.Columns.Add("Time Taken (min)", 120);

            // Query to get the signed-in client's requests
            string query = @"
                SELECT tr.id, t.TaskName, l.Area, ts.DayOfWeek, ts.StartTime, ts.EndTime, tr.Status,
                       w.Name AS WorkerName, tr.RequestedDateTime, ta.ActualDurationMinutes
                FROM TaskRequests tr
                JOIN Tasks t ON tr.TaskID = t.id
                JOIN Locations l ON tr.LocationID = l.id
                JOIN TimeSlots ts ON tr.PreferredTimeSlot = ts.id
                LEFT JOIN TaskAssignments ta ON tr.id = ta.RequestID
                LEFT JOIN Workers w ON ta.WorkerID = w.id
                WHERE tr.ClientID = @ClientId
                ORDER BY tr.RequestedDateTime DESC";
            var dt = _databaseService.ExecuteQuery(query, cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value));

            foreach (DataRow row in dt.Rows)
            {
                string slot = $"Day {row["DayOfWeek"]}: {row["StartTime"]}-{row["EndTime"]}";
                string status = row["Status"]?.ToString() ?? "";
                string worker = row["WorkerName"] == DBNull.Value ? "Not assigned" : (row["WorkerName"]?.ToString() ?? "Not assigned");
                string requestedOn = Convert.ToDateTime(row["RequestedDateTime"]).ToString("MMM dd, yyyy HH:mm");
                string timeTaken = string.Empty;
                if (row.Table.Columns.Contains("ActualDurationMinutes") && row["ActualDurationMinutes"] != DBNull.Value && row["ActualDurationMinutes"] != null)
                {
                    timeTaken = row["ActualDurationMinutes"].ToString() ?? string.Empty;
                }
                // Only show assigned or completed
                if (status != "assigned" && status != "completed") continue;
                var item = new ListViewItem(new[]
                {
                    row["TaskName"].ToString() ?? "",
                    row["Area"].ToString() ?? "",
                    slot,
                    status,
                    worker,
                    requestedOn,
                    timeTaken
                });
                if (status == "completed")
                    item.BackColor = System.Drawing.Color.PaleGreen;
                else if (status == "assigned")
                    item.BackColor = System.Drawing.Color.LightSkyBlue;
                lvRequests.Items.Add(item);
            }

            tab.Controls.Add(lblTitle);
            tab.Controls.Add(lvRequests);
        }

        // Class for task items in the dropdown
        private class TaskItem
        {
            public int Id { get; }
            public string Name { get; }
            public string Duration { get; }
            public string Fee { get; }

            public TaskItem(int id, string name, string duration, string fee)
            {
                Id = id;
                Name = name;
                Duration = duration;
                Fee = fee;
            }

            public override string ToString() => Name;
        }

        private void OpenTaskRequestForm(int taskId)
        {
            if (_loggedInClientId == null)
            {
                MessageBox.Show("You must be logged in to request a task.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form requestForm = new Form
            {
                Text = "Request Task",
                Size = new System.Drawing.Size(420, 300),
                StartPosition = FormStartPosition.CenterParent
            };

            Label lblAddress = new Label { Text = "Request Address:", Location = new System.Drawing.Point(20, 20) };
            TextBox txtAddress = new TextBox { Location = new System.Drawing.Point(150, 20), Width = 200 };

            // Add location selection
            Label lblLocation = new Label { Text = "Location Area:", Location = new System.Drawing.Point(20, 60) };
            ComboBox cmbLocations = new ComboBox
            {
                Location = new System.Drawing.Point(150, 60),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            DataTable locations = _workerService.GetAllLocations();
            foreach (DataRow row in locations.Rows)
                cmbLocations.Items.Add(new ComboBoxItem(row["Area"].ToString() ?? "", Convert.ToInt32(row["Id"])));
            if (cmbLocations.Items.Count > 0)
                cmbLocations.SelectedIndex = 0;

            // Time slot selection
            Label lblTimeSlot = new Label { Text = "Time Slot:", Location = new System.Drawing.Point(20, 100) };
            ComboBox cmbTimeSlots = new ComboBox { Location = new System.Drawing.Point(150, 100), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            // Label to show available workers
            Label lblAvailableWorkers = new Label { Text = "Available Workers:", Location = new System.Drawing.Point(20, 140), Width = 370, Height = 40, AutoSize = false };

            // Helper to load available time slots for the selected task/location
            void LoadAvailableTimeSlots()
            {
                cmbTimeSlots.Items.Clear();
                if (cmbLocations.SelectedItem == null) return;
                int locationId = ((ComboBoxItem)cmbLocations.SelectedItem).Value;
                var availableSlotIds = _workerService.GetAvailableTimeSlotsForTask(taskId, locationId);
                DataTable slots = _workerService.GetAllTimeSlots();
                foreach (DataRow row in slots.Rows)
                {
                    int slotId = Convert.ToInt32(row["Id"]);
                    if (availableSlotIds.Contains(slotId))
                    {
                        string slotText = $"Day {row["DayOfWeek"]}: {row["StartTime"]}-{row["EndTime"]}";
                        cmbTimeSlots.Items.Add(new ComboBoxItem(slotText, slotId));
                    }
                }
                if (cmbTimeSlots.Items.Count > 0)
                    cmbTimeSlots.SelectedIndex = 0;
                UpdateAvailableWorkersLabel();
            }

            void UpdateAvailableWorkersLabel()
            {
                lblAvailableWorkers.Text = "Available Workers:";
                if (cmbLocations.SelectedItem == null || cmbTimeSlots.SelectedItem == null) return;
                int locationId = ((ComboBoxItem)cmbLocations.SelectedItem).Value;
                int timeSlotId = ((ComboBoxItem)cmbTimeSlots.SelectedItem).Value;
                var workerIds = _workerService.GetAvailableWorkersForTask(taskId, locationId, timeSlotId);
                if (workerIds.Count == 0)
                {
                    lblAvailableWorkers.Text = "Available Workers: None";
                }
                else
                {
                    // Get worker names
                    var allWorkers = _workerService.GetAllWorkers();
                    var names = allWorkers.Rows.Cast<DataRow>()
                        .Where(r => workerIds.Contains(Convert.ToInt32(r["Id"])))
                        .Select(r => r["Name"].ToString())
                        .ToList();
                    lblAvailableWorkers.Text = "Available Workers: " + string.Join(", ", names);
                }
            }

            cmbLocations.SelectedIndexChanged += (s, e) => LoadAvailableTimeSlots();
            cmbTimeSlots.SelectedIndexChanged += (s, e) => UpdateAvailableWorkersLabel();
            LoadAvailableTimeSlots();

            Button btnSubmit = new Button { Text = "Submit Request", Location = new System.Drawing.Point(150, 200), Width = 150 };
            btnSubmit.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtAddress.Text))
                {
                    MessageBox.Show("Request address is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cmbLocations.SelectedItem == null)
                {
                    MessageBox.Show("Please select a location area.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cmbTimeSlots.SelectedItem == null)
                {
                    MessageBox.Show("Please select a time slot.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    int locationId = ((ComboBoxItem)cmbLocations.SelectedItem).Value;
                    int timeSlotId = ((ComboBoxItem)cmbTimeSlots.SelectedItem).Value;
                    string? assignedWorkerName;
                    _taskCatalogService.CreateTaskRequest(
                        taskId,
                        _loggedInClientId.Value,
                        txtAddress.Text,
                        timeSlotId,
                        locationId,
                        out assignedWorkerName);
                    if (!string.IsNullOrEmpty(assignedWorkerName))
                    {
                        MessageBox.Show($"Task request submitted and assigned to {assignedWorkerName}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Task request submitted, but no available worker was found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    requestForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error submitting task request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            requestForm.Controls.Add(lblAddress);
            requestForm.Controls.Add(txtAddress);
            requestForm.Controls.Add(lblLocation);
            requestForm.Controls.Add(cmbLocations);
            requestForm.Controls.Add(lblTimeSlot);
            requestForm.Controls.Add(cmbTimeSlots);
            requestForm.Controls.Add(lblAvailableWorkers);
            requestForm.Controls.Add(btnSubmit);

            requestForm.ShowDialog();
        }

        private void EnsureDatabaseCreated()
        {
            try
            {
                _databaseService.EnsureDatabaseInitialized();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper for checked list box
        private class ComboBoxItem
        {
            public string Text { get; }
            public int Value { get; }
            public ComboBoxItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }

        private void SetupWorkerLoginTab(TabPage tab)
        {
            AddBackToMenuButton(tab);
            int yOffset = 50;
            Button btnLogin = new Button { Text = "Login", Location = new System.Drawing.Point(150, 30 + yOffset), Width = 120 };
            Button btnCreate = new Button { Text = "Create Profile", Location = new System.Drawing.Point(300, 30 + yOffset), Width = 120 };
            Label lblSelect = new Label { Text = "Select Worker:", Location = new System.Drawing.Point(30, 80 + yOffset) };
            ComboBox cmbWorkers = new ComboBox { Location = new System.Drawing.Point(150, 80 + yOffset), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnDoLogin = new Button { Text = "Login", Location = new System.Drawing.Point(370, 80 + yOffset), Width = 100 };
            Label lblName = new Label { Text = "Name:", Location = new System.Drawing.Point(30, 140 + yOffset) };
            TextBox txtName = new TextBox { Location = new System.Drawing.Point(170, 140 + yOffset), Width = 200 };
            Label lblPhone = new Label { Text = "Phone:", Location = new System.Drawing.Point(30, 180 + yOffset) };
            TextBox txtPhone = new TextBox { Location = new System.Drawing.Point(170, 180 + yOffset), Width = 200 };
            Label lblEmail = new Label { Text = "Email:", Location = new System.Drawing.Point(30, 220 + yOffset) };
            TextBox txtEmail = new TextBox { Location = new System.Drawing.Point(170, 220 + yOffset), Width = 200 };
            Button btnSubmit = new Button { Text = "Submit", Location = new System.Drawing.Point(170, 260 + yOffset) };

            void ShowLoginUI()
            {
                cmbWorkers.Items.Clear();
                foreach (DataRow row in _workerService.GetAllWorkers().Rows)
                    cmbWorkers.Items.Add(new ComboBoxItem(row["Name"]?.ToString() ?? "", (int)row["Id"]));
                lblSelect.Visible = true; cmbWorkers.Visible = true; btnDoLogin.Visible = true;
                lblName.Visible = false; txtName.Visible = false;
                lblPhone.Visible = false; txtPhone.Visible = false;
                lblEmail.Visible = false; txtEmail.Visible = false;
                btnSubmit.Visible = false;
            }
            void ShowCreateUI()
            {
                lblSelect.Visible = false; cmbWorkers.Visible = false; btnDoLogin.Visible = false;
                lblName.Visible = true; txtName.Visible = true;
                lblPhone.Visible = true; txtPhone.Visible = true;
                lblEmail.Visible = true; txtEmail.Visible = true;
                btnSubmit.Visible = true;
                txtName.Text = ""; txtPhone.Text = ""; txtEmail.Text = "";
            }
            btnLogin.Click += (s, e) => ShowLoginUI();
            btnCreate.Click += (s, e) => ShowCreateUI();
            btnDoLogin.Click += (s, e) =>
            {
                if (cmbWorkers.SelectedItem is ComboBoxItem workerItem)
                {
                    var profile = _workerService.GetWorkerProfile(workerItem.Value);
                    txtName.Text = profile.Name;
                    txtPhone.Text = profile.Phone;
                    txtEmail.Text = profile.Email;
                    _loggedInWorkerId = workerItem.Value;
                    MessageBox.Show("Logged in as " + profile.Name);
                    this.Controls.Remove(tabControl!);
                    ShowTabs("worker");
                }
            };
            btnSubmit.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPhone.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Name, phone, and email are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _workerService.SaveOrUpdateProfile(null, txtName.Text, txtPhone.Text, txtEmail.Text, new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>());
                MessageBox.Show("Profile created. Please login.");
                ShowLoginUI();
            };
            tab.Controls.Add(btnLogin); tab.Controls.Add(btnCreate);
            tab.Controls.Add(lblSelect); tab.Controls.Add(cmbWorkers); tab.Controls.Add(btnDoLogin);
            tab.Controls.Add(lblName); tab.Controls.Add(txtName);
            tab.Controls.Add(lblPhone); tab.Controls.Add(txtPhone);
            tab.Controls.Add(lblEmail); tab.Controls.Add(txtEmail);
            tab.Controls.Add(btnSubmit);
            // Hide all except login/create buttons at start
            lblSelect.Visible = false; cmbWorkers.Visible = false; btnDoLogin.Visible = false;
            lblName.Visible = false; txtName.Visible = false;
            lblPhone.Visible = false; txtPhone.Visible = false;
            lblEmail.Visible = false; txtEmail.Visible = false;
            btnSubmit.Visible = false;
        }

        private void LoadAssignedTasks(ListView lvAssigned)
        {
            lvAssigned.Items.Clear();
            int workerId = _loggedInWorkerId ?? 0;
            var dt = _databaseService.ExecuteQuery(@"
                SELECT t.TaskName, l.Area, ts.DayOfWeek, ts.StartTime, ts.EndTime, tr.Status, tr.id as RequestID,
                       ta.StartedTime, ta.ActualTimeSlot, ta.ActualDurationMinutes
                FROM TaskAssignments ta
                JOIN TaskRequests tr ON ta.RequestID = tr.id
                JOIN Tasks t ON tr.TaskID = t.id
                JOIN Locations l ON tr.LocationID = l.id
                JOIN TimeSlots ts ON tr.PreferredTimeSlot = ts.id
                WHERE ta.WorkerID = @W AND tr.Status <> 'open'",
                cmd => cmd.Parameters.AddWithValue("@W", workerId));

            foreach (DataRow row in dt.Rows)
            {
                string slot = $"Day {row["DayOfWeek"]}: {row["StartTime"]}-{row["EndTime"]}";
                string startedTime = row["StartedTime"] == DBNull.Value ? "" : Convert.ToDateTime(row["StartedTime"]).ToString("g");
                string duration = string.Empty;
                if (!(row["ActualDurationMinutes"] is DBNull) && row["ActualDurationMinutes"] != null)
                {
                    duration = row["ActualDurationMinutes"].ToString() ?? string.Empty;
                }
                var item = new ListViewItem(new[]
                {
                    row["TaskName"].ToString() ?? "",
                    row["Area"].ToString() ?? "",
                    slot,
                    row["Status"].ToString() ?? "",
                    row["RequestID"].ToString() ?? "",
                    startedTime,
                    duration
                });
                lvAssigned.Items.Add(item);
            }
        }

        private void SetupWorkerAssignedTasksTab(TabPage tab)
        {
            AddBackToMenuButton(tab);
            if (_loggedInWorkerId == null) return;
            int yOffset = 30;
            Label lblTitle = new Label
            {
                Text = "Assigned Tasks:",
                Location = new System.Drawing.Point(20, 20 + yOffset),
                Width = 200,
                Font = new System.Drawing.Font(Font.FontFamily, 12, System.Drawing.FontStyle.Bold)
            };
            ListView lvAssigned = new ListView
            {
                Location = new System.Drawing.Point(20, 50 + yOffset),
                Size = new System.Drawing.Size(800, 300),
                View = View.Details,
                FullRowSelect = true
            };
            lvAssigned.Columns.Add("Task Name", 200);
            lvAssigned.Columns.Add("Location", 150);
            lvAssigned.Columns.Add("Time Slot", 150);
            lvAssigned.Columns.Add("Status", 100);
            lvAssigned.Columns.Add("Request ID", 0);
            lvAssigned.Columns.Add("Started Time", 150);
            lvAssigned.Columns.Add("Duration (min)", 120);
            LoadAssignedTasks(lvAssigned);

            // Add "Mark as Completed" button
            Button btnComplete = new Button
            {
                Text = "Mark as Completed",
                Location = new System.Drawing.Point(20, 360 + yOffset),
                Width = 150,
                Enabled = false
            };

            // Add "Mark as In Progress" button
            Button btnInProgress = new Button
            {
                Text = "Mark as In Progress",
                Location = new System.Drawing.Point(180, 360 + yOffset),
                Width = 170,
                Enabled = false
            };

            // Enable/disable button based on selection and status
            lvAssigned.SelectedIndexChanged += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    string status = lvAssigned.SelectedItems[0].SubItems[3].Text;
                    btnComplete.Enabled = (status == "assigned" || status == "scheduled" || status == "in_progress");
                    btnInProgress.Enabled = (status == "assigned" || status == "scheduled");
                }
                else
                {
                    btnComplete.Enabled = false;
                    btnInProgress.Enabled = false;
                }
            };

            // Handle completion button click
            btnComplete.Click += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    var selectedItem = lvAssigned.SelectedItems[0];
                    string taskName = selectedItem.SubItems[0].Text;
                    int requestId = int.Parse(selectedItem.SubItems[4].Text); // Get the request ID

                    if (MessageBox.Show($"Are you sure you want to mark '{taskName}' as completed?",
                                        "Confirm Task Completion",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        bool success = _workerService.MarkTaskAsCompleted(requestId, _loggedInWorkerId.Value);

                        if (success)
                        {
                            LoadAssignedTasks(lvAssigned); // Reload from DB to show updated duration/status
                            btnComplete.Enabled = false;
                            MessageBox.Show("Task marked as completed successfully.",
                                            "Success",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to mark task as completed. Please try again.",
                                            "Error",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                        }
                    }
                }
            };

            // Handle in-progress button click
            btnInProgress.Click += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    var selectedItem = lvAssigned.SelectedItems[0];
                    int requestId = int.Parse(selectedItem.SubItems[4].Text);
                    try
                    {
                        bool success = _workerService.MarkTaskAsInProgress(requestId, _loggedInWorkerId.Value);
                        if (success)
                        {
                            LoadAssignedTasks(lvAssigned); // Reload from DB to show updated StartedTime
                            btnInProgress.Enabled = false;
                            MessageBox.Show("Task marked as in progress.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            // Check if the StartedTime is already set in the DB and show a more helpful message
                            var startedTimeObj = _databaseService.ExecuteQueryScalar(
                                "SELECT StartedTime FROM TaskAssignments WHERE RequestID = @RequestId AND WorkerID = @WorkerId",
                                cmd => {
                                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                                    cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value);
                                });
                            if (startedTimeObj != DBNull.Value && startedTimeObj != null)
                            {
                                MessageBox.Show("Task is currently in progress.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadAssignedTasks(lvAssigned);
                            }
                            else
                            {
                                MessageBox.Show("Failed to mark task as in progress. The task may already be in progress or completed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            tab.Controls.Add(lblTitle);
            tab.Controls.Add(lvAssigned);
            tab.Controls.Add(btnComplete);
            tab.Controls.Add(btnInProgress);
        }
    }
}