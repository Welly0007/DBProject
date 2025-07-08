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
                "Server=Welly-pc\\SQLEXPRESS;Database=TaskWorkerDB;Trusted_Connection=True;TrustServerCertificate=True;",
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

            // Add "Generate Report" button (visible only when logged in)
            Button btnGenerateReport = new Button
            {
                Text = "Generate Report",
                Location = new System.Drawing.Point(170, 400 + yOffset),
                Width = 150,
                Visible = _loggedInClientId != null
            };

            btnGenerateReport.Click += (s, e) =>
            {
                if (_loggedInClientId == null) return;

                Form reportForm = new Form
                {
                    Text = "Client Report",
                    Size = new System.Drawing.Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                ListView lvReport = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };

                lvReport.Columns.Add("Metric", 300);
                lvReport.Columns.Add("Value", 400);

                // Query to get report data
                var totalRequests = _databaseService.ExecuteQueryScalar(
                    "SELECT COUNT(*) FROM TaskRequests WHERE ClientID = @ClientId",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "0";

                var completedRequests = _databaseService.ExecuteQueryScalar(
                    "SELECT COUNT(*) FROM TaskRequests WHERE ClientID = @ClientId AND Status = 'completed'",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "0";

                var totalMoneySpent = _databaseService.ExecuteQueryScalar(@"
                    SELECT ISNULL(SUM(t.AverageFee), 0)
                    FROM TaskRequests tr
                    JOIN Tasks t ON tr.TaskID = t.id
                    WHERE tr.ClientID = @ClientId AND tr.Status = 'completed'",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "0";

                var mostDealtWorker = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 w.Name
                    FROM TaskRequests tr
                    JOIN TaskAssignments ta ON tr.id = ta.RequestID
                    JOIN Workers w ON ta.WorkerID = w.id
                    WHERE tr.ClientID = @ClientId
                    GROUP BY w.Name
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "N/A";

                var averageRating = _databaseService.ExecuteQueryScalar(@"
                    SELECT ISNULL(AVG(RatingValue), 0)
                    FROM ClientRatings
                    WHERE ClientID = @ClientId",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "0";

                var mostRequestedSpecialty = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 s.Name
                    FROM TaskRequests tr
                    JOIN Tasks t ON tr.TaskID = t.id
                    JOIN Specialties s ON t.SpecialtyID = s.id
                    WHERE tr.ClientID = @ClientId
                    GROUP BY s.Name
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "N/A";

                var mostCondensedLocation = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 l.Area
                    FROM TaskRequests tr
                    JOIN Locations l ON tr.LocationID = l.id
                    WHERE tr.ClientID = @ClientId
                    GROUP BY l.Area
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "N/A";

                var busiestTimeslot = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 CONCAT('Day ', ts.DayOfWeek, ': ', ts.StartTime, '-', ts.EndTime)
                    FROM TaskRequests tr
                    JOIN TimeSlots ts ON tr.PreferredTimeSlot = ts.id
                    WHERE tr.ClientID = @ClientId
                    GROUP BY ts.DayOfWeek, ts.StartTime, ts.EndTime
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@ClientId", _loggedInClientId.Value))?.ToString() ?? "N/A";

                lvReport.Items.Add(new ListViewItem(new[] { "Total Requests", totalRequests }));
                lvReport.Items.Add(new ListViewItem(new[] { "Completed Requests", completedRequests }));
                lvReport.Items.Add(new ListViewItem(new[] { "Total Money Spent ($)", totalMoneySpent }));
                lvReport.Items.Add(new ListViewItem(new[] { "Most Dealt Worker", mostDealtWorker }));
                lvReport.Items.Add(new ListViewItem(new[] { "Your Average Rating", averageRating }));
                lvReport.Items.Add(new ListViewItem(new[] { "Most Requested Specialty", mostRequestedSpecialty }));
                lvReport.Items.Add(new ListViewItem(new[] { "Most Condensed Location", mostCondensedLocation }));
                lvReport.Items.Add(new ListViewItem(new[] { "Busiest Timeslot", busiestTimeslot }));

                reportForm.Controls.Add(lvReport);
                reportForm.ShowDialog();
            };

            tab.Controls.Add(btnGenerateReport);

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

            // Add "Generate Report" button (visible only when logged in)
            Button btnGenerateReport = new Button
            {
                Text = "Generate Report",
                Location = new System.Drawing.Point(170, 450 + yOffset),
                Width = 150,
                Visible = _loggedInWorkerId != null
            };

            btnGenerateReport.Click += (s, e) =>
            {
                if (_loggedInWorkerId == null) return;

                Form reportForm = new Form
                {
                    Text = "Worker Report",
                    Size = new System.Drawing.Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                ListView lvReport = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true
                };

                lvReport.Columns.Add("Metric", 300);
                lvReport.Columns.Add("Value", 400);

                // Query and populate report data
                var totalTasks = _databaseService.ExecuteQueryScalar(
                    "SELECT COUNT(*) FROM TaskAssignments WHERE WorkerID = @WorkerId",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "0";

                var completedTasks = _databaseService.ExecuteQueryScalar(
                    "SELECT COUNT(*) FROM TaskAssignments WHERE WorkerID = @WorkerId AND Status = 'completed'",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "0";

                var totalMoneyEarned = _databaseService.ExecuteQueryScalar(@"
                    SELECT ISNULL(SUM(t.AverageFee), 0)
                    FROM TaskAssignments ta
                    JOIN TaskRequests tr ON ta.RequestID = tr.id
                    JOIN Tasks t ON tr.TaskID = t.id
                    WHERE ta.WorkerID = @WorkerId AND ta.Status = 'completed'",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "0";

                var mostFrequentClient = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 c.Name
                    FROM TaskRequests tr
                    JOIN TaskAssignments ta ON tr.id = ta.RequestID
                    JOIN Clients c ON tr.ClientID = c.id
                    WHERE ta.WorkerID = @WorkerId
                    GROUP BY c.Name
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "N/A";

                var averageClientRating = _databaseService.ExecuteQueryScalar(@"
                    SELECT ISNULL(AVG(RatingValue), 0)
                    FROM WorkerRatings
                    WHERE workerID = @WorkerId",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "0";

                var mostRequestedSpecialty = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 s.Name
                    FROM TaskRequests tr
                    JOIN Tasks t ON tr.TaskID = t.id
                    JOIN Specialties s ON t.SpecialtyID = s.id
                    JOIN TaskAssignments ta ON tr.id = ta.RequestID
                    WHERE ta.WorkerID = @WorkerId
                    GROUP BY s.Name
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "N/A";

                var mostCondensedLocation = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 l.Area
                    FROM TaskRequests tr
                    JOIN Locations l ON tr.LocationID = l.id
                    JOIN TaskAssignments ta ON tr.id = ta.RequestID
                    WHERE ta.WorkerID = @WorkerId
                    GROUP BY l.Area
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "N/A";

                var busiestTimeslot = _databaseService.ExecuteQueryScalar(@"
                    SELECT TOP 1 CONCAT('Day ', ts.DayOfWeek, ': ', ts.StartTime, '-', ts.EndTime)
                    FROM TaskRequests tr
                    JOIN TimeSlots ts ON tr.PreferredTimeSlot = ts.id
                    JOIN TaskAssignments ta ON tr.id = ta.RequestID
                    WHERE ta.WorkerID = @WorkerId
                    GROUP BY ts.DayOfWeek, ts.StartTime, ts.EndTime
                    ORDER BY COUNT(*) DESC",
                    cmd => cmd.Parameters.AddWithValue("@WorkerId", _loggedInWorkerId.Value))?.ToString() ?? "N/A";

                lvReport.Items.Add(new ListViewItem(new[] { "Total Tasks", totalTasks }));
                lvReport.Items.Add(new ListViewItem(new[] { "Completed Tasks", completedTasks }));
                lvReport.Items.Add(new ListViewItem(new[] { "Total Money Earned ($)", totalMoneyEarned }));
                lvReport.Items.Add(new ListViewItem(new[] { "Most Frequent Client", mostFrequentClient }));
                lvReport.Items.Add(new ListViewItem(new[] { "Your Average Rating", averageClientRating }));
                lvReport.Items.Add(new ListViewItem(new[] { "Most Requested Specialty", mostRequestedSpecialty }));
                lvReport.Items.Add(new ListViewItem(new[] { "Most Condensed Location", mostCondensedLocation }));
                lvReport.Items.Add(new ListViewItem(new[] { "Busiest Timeslot", busiestTimeslot }));

                reportForm.Controls.Add(lvReport);
                reportForm.ShowDialog();
            };

            tab.Controls.Add(btnGenerateReport);

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
            lvRequests.Columns.Add("Rating", 100); // New column for client rating

            // Query to get the signed-in client's requests
            string query = @"
                SELECT tr.id, t.TaskName, l.Area, ts.DayOfWeek, ts.StartTime, ts.EndTime, tr.Status,
                       w.Name AS WorkerName, tr.RequestedDateTime, ta.ActualDurationMinutes,
                       cr.RatingValue AS ClientRating, cr.Feedback
                FROM TaskRequests tr
                JOIN Tasks t ON tr.TaskID = t.id
                JOIN Locations l ON tr.LocationID = l.id
                JOIN TimeSlots ts ON tr.PreferredTimeSlot = ts.id
                LEFT JOIN TaskAssignments ta ON tr.id = ta.RequestID
                LEFT JOIN Workers w ON ta.WorkerID = w.id
                LEFT JOIN ClientRatings cr ON cr.RequestID = tr.id
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
                string rating = (row["ClientRating"] == DBNull.Value || row["ClientRating"] == null) ? "Not rated" : (row["ClientRating"].ToString() ?? "Not rated");
                string feedback = row["Feedback"]?.ToString() ?? "";

                var item = new ListViewItem(new string[]
                {
                    (row["TaskName"]?.ToString() ?? ""),
                    (row["Area"]?.ToString() ?? ""),
                    (slot ?? ""),
                    (status ?? ""),
                    (worker ?? ""),
                    (requestedOn ?? ""),
                    (timeTaken ?? ""),
                    (rating ?? "") // Add rating to the list view
                })
                {
                    Tag = feedback // Store feedback in the Tag property
                };

                if (status == "completed")
                    item.BackColor = System.Drawing.Color.PaleGreen;
                else if (status == "assigned")
                    item.BackColor = System.Drawing.Color.LightSkyBlue;

                lvRequests.Items.Add(item);
            }

            Button btnShowFeedback = new Button
            {
                Text = "Show Feedback",
                Location = new System.Drawing.Point(20, 370 + yOffset),
                Width = 150,
                Enabled = false
            };

            // Add 'Rate Worker' button
            Button btnRateWorker = new Button
            {
                Text = "Rate Worker",
                Location = new System.Drawing.Point(180, 370 + yOffset),
                Width = 150,
                Enabled = false
            };

            lvRequests.SelectedIndexChanged += (s, e) =>
            {
                if (lvRequests.SelectedItems.Count > 0)
                {
                    string status = lvRequests.SelectedItems[0].SubItems[3].Text;
                    string worker = lvRequests.SelectedItems[0].SubItems[4].Text;
                    bool canRateWorker = false;
                    if (status == "completed" && worker != "Not assigned")
                    {
                        int listIndex = lvRequests.SelectedItems[0].Index;
                        int requestId = -1;
                        int taskId = -1;
                        int workerId = -1;
                        if (dt.Rows.Count > listIndex)
                        {
                            requestId = Convert.ToInt32(dt.Rows[listIndex]["id"]);
                            // Get TaskID for this request
                            var reqTask = _databaseService.ExecuteQuery("SELECT TaskID FROM TaskRequests WHERE id = @R", cmd => cmd.Parameters.AddWithValue("@R", requestId));
                            if (reqTask.Rows.Count > 0)
                                taskId = Convert.ToInt32(reqTask.Rows[0]["TaskID"]);
                            if (dt.Rows[listIndex]["WorkerName"] != DBNull.Value)
                            {
                                var workerNameDb = dt.Rows[listIndex]["WorkerName"].ToString();
                                var wdt = _databaseService.ExecuteQuery("SELECT id FROM Workers WHERE Name = @N", cmd => cmd.Parameters.AddWithValue("@N", workerNameDb));
                                if (wdt.Rows.Count > 0)
                                    workerId = Convert.ToInt32(wdt.Rows[0]["id"]);
                            }
                        }
                        // Check if a WorkerRating already exists for this TaskID, WorkerID, and RequestID
                        if (taskId != -1 && workerId != -1 && _loggedInClientId != null && requestId != -1)
                        {
                            var wrdt = _databaseService.ExecuteQuery("SELECT 1 FROM WorkerRatings WHERE WorkerID = @W AND TaskID = @T AND RequestID = @RId", cmd =>
                            {
                                cmd.Parameters.AddWithValue("@W", workerId);
                                cmd.Parameters.AddWithValue("@T", taskId);
                                cmd.Parameters.AddWithValue("@RId", requestId);
                            });
                            canRateWorker = wrdt.Rows.Count == 0;
                        }
                    }
                    btnRateWorker.Enabled = canRateWorker;
                    // Feedback button logic remains unchanged
                    string rating = lvRequests.SelectedItems[0].SubItems[7].Text;
                    btnShowFeedback.Enabled = rating != "Not rated";
                }
                else
                {
                    btnShowFeedback.Enabled = false;
                    btnRateWorker.Enabled = false;
                }
            };

            btnShowFeedback.Click += (s, e) =>
            {
                if (lvRequests.SelectedItems.Count > 0)
                {
                    string feedback = lvRequests.SelectedItems[0].Tag?.ToString() ?? "No feedback available.";
                    MessageBox.Show(feedback, "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnRateWorker.Click += (s, e) =>
            {
                if (lvRequests.SelectedItems.Count == 0) return;
                var item = lvRequests.SelectedItems[0];
                string workerName = item.SubItems[4].Text;
                if (workerName == "Not assigned")
                {
                    MessageBox.Show("No worker assigned to this task.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                int listIndex = item.Index;
                int requestId = -1;
                int workerId = -1;
                int taskId = -1;
                if (dt.Rows.Count > listIndex)
                {
                    requestId = Convert.ToInt32(dt.Rows[listIndex]["id"]);
                    // Get TaskID for this request
                    var reqTask = _databaseService.ExecuteQuery("SELECT TaskID FROM TaskRequests WHERE id = @R", cmd => cmd.Parameters.AddWithValue("@R", requestId));
                    if (reqTask.Rows.Count > 0)
                        taskId = Convert.ToInt32(reqTask.Rows[0]["TaskID"]);
                    // Get WorkerID for this request
                    if (dt.Rows[listIndex]["WorkerName"] != DBNull.Value)
                    {
                        var workerNameDb = dt.Rows[listIndex]["WorkerName"].ToString();
                        var wdt = _databaseService.ExecuteQuery("SELECT id FROM Workers WHERE Name = @N", cmd => cmd.Parameters.AddWithValue("@N", workerNameDb));
                        if (wdt.Rows.Count > 0)
                            workerId = Convert.ToInt32(wdt.Rows[0]["id"]);
                    }
                }
                if (requestId == -1 || workerId == -1 || taskId == -1)
                {
                    MessageBox.Show("Could not find worker assignment for this request.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Form rateForm = new Form { Text = $"Rate Worker: {workerName}", Size = new System.Drawing.Size(350, 250), StartPosition = FormStartPosition.CenterParent };
                Label lbl = new Label { Text = "Rating (1-5):", Location = new System.Drawing.Point(20, 20) };
                NumericUpDown nud = new NumericUpDown { Minimum = 1, Maximum = 5, Location = new System.Drawing.Point(120, 20), Width = 50 };
                Label lblFb = new Label { Text = "Feedback:", Location = new System.Drawing.Point(20, 60) };
                TextBox txtFb = new TextBox { Location = new System.Drawing.Point(20, 90), Width = 280, Height = 60, Multiline = true };
                Button btnSubmit = new Button { Text = "Submit", Location = new System.Drawing.Point(120, 170), Width = 80 };
                btnSubmit.Click += (s2, e2) =>
                {
                    decimal ratingVal = nud.Value;
                    string feedback = txtFb.Text;
                    // Save to WorkerRatings with correct RequestID 
                    // FOCUS INSERT1
                    _databaseService.ExecuteNonQuery(@"INSERT INTO WorkerRatings (WorkerID, TaskID, RequestID, RatingValue, Date, Feedback) VALUES (@W, @T, @RId, @R, GETDATE(), @F)", cmd =>
                    {
                        cmd.Parameters.AddWithValue("@W", workerId);
                        cmd.Parameters.AddWithValue("@T", taskId);
                        cmd.Parameters.AddWithValue("@RId", requestId);
                        cmd.Parameters.AddWithValue("@R", ratingVal);
                        cmd.Parameters.AddWithValue("@F", feedback);
                    });
                    MessageBox.Show("Thank you for rating the worker!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    rateForm.Close();
                    tab.Controls.Clear();
                    SetupClientRequestsTab(tab);
                };
                rateForm.Controls.Add(lbl);
                rateForm.Controls.Add(nud);
                rateForm.Controls.Add(lblFb);
                rateForm.Controls.Add(txtFb);
                rateForm.Controls.Add(btnSubmit);
                rateForm.ShowDialog();
            };

            tab.Controls.Add(btnShowFeedback);


            tab.Controls.Add(lblTitle);
            tab.Controls.Add(lvRequests);
            tab.Controls.Add(btnShowFeedback);
            tab.Controls.Add(btnRateWorker);
        }


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


            Label lblTimeSlot = new Label { Text = "Time Slot:", Location = new System.Drawing.Point(20, 100) };
            ComboBox cmbTimeSlots = new ComboBox { Location = new System.Drawing.Point(150, 100), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            // Label to show available workers
            Label lblAvailableWorkers = new Label { Text = "Available Workers:", Location = new System.Drawing.Point(20, 140), Width = 370, Height = 40, AutoSize = false };


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

                    if (tabControl != null)
                    {
                        foreach (TabPage tab in tabControl.TabPages)
                        {
                            if (tab.Text == "My Requests")
                            {
                                tab.Controls.Clear();
                                SetupClientRequestsTab(tab);
                                break;
                            }
                        }
                    }
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
            lvAssigned.Columns.Add("Fee ($)", 100);
            lvAssigned.Columns.Add("Status", 100);
            lvAssigned.Columns.Add("Request ID", 0);
            lvAssigned.Columns.Add("Started Time", 150);
            lvAssigned.Columns.Add("Duration (min)", 120);
            lvAssigned.Columns.Add("Rating", 80);
            LoadAssignedTasksWithRating(lvAssigned);


            Label lblTotalMoney = new Label
            {
                Text = "Total Money Earned: $0.00",
                Location = new System.Drawing.Point(20, 360 + yOffset),
                Width = 300,
                Font = new System.Drawing.Font(Font.FontFamily, 10, System.Drawing.FontStyle.Bold)
            };
            void UpdateTotalMoney()
            {
                int workerId = _loggedInWorkerId ?? 0;
                var dt = _databaseService.ExecuteQuery(@"
                    SELECT SUM(t.AverageFee) AS TotalEarned
                    FROM TaskAssignments ta
                    JOIN TaskRequests tr ON ta.RequestID = tr.id
                    JOIN Tasks t ON tr.TaskID = t.id
                    WHERE ta.WorkerID = @W AND ta.Status = 'completed'",
                    cmd => cmd.Parameters.AddWithValue("@W", workerId));
                decimal total = 0;
                if (dt.Rows.Count > 0 && dt.Rows[0]["TotalEarned"] != DBNull.Value)
                    total = Convert.ToDecimal(dt.Rows[0]["TotalEarned"]);
                lblTotalMoney.Text = $"Total Money Earned: ${total:F2}";
            }
            UpdateTotalMoney();

            // Add "Mark as Completed" button
            Button btnComplete = new Button
            {
                Text = "Mark as Completed",
                Location = new System.Drawing.Point(20, 400 + yOffset),
                Width = 150,
                Enabled = false
            };

            // Add "Mark as In Progress" button
            Button btnInProgress = new Button
            {
                Text = "Mark as In Progress",
                Location = new System.Drawing.Point(180, 400 + yOffset),
                Width = 170,
                Enabled = false
            };

            // Add "Rate Client" button
            Button btnRateClient = new Button
            {
                Text = "Rate Client",
                Location = new System.Drawing.Point(360, 400 + yOffset),
                Width = 150,
                Enabled = false
            };

            // Add Show Feedback button
            Button btnShowFeedback = new Button
            {
                Text = "Show Feedback",
                Location = new System.Drawing.Point(540, 400 + yOffset),
                Width = 150,
                Enabled = false
            };


            lvAssigned.SelectedIndexChanged += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    string status = lvAssigned.SelectedItems[0].SubItems[3].Text;
                    int requestId = int.Parse(lvAssigned.SelectedItems[0].SubItems[4].Text);
                    btnComplete.Enabled = (status == "assigned" || status == "scheduled" || status == "in_progress");
                    btnInProgress.Enabled = (status == "assigned" || status == "scheduled");
                    // Check if the task is completed and not rated
                    var dt = _databaseService.ExecuteQuery(@"
                        SELECT COUNT(*) AS RatingExists
                        FROM ClientRatings cr
                        WHERE cr.RequestID = @RequestId",
                        cmd => cmd.Parameters.AddWithValue("@RequestId", requestId));
                    bool isRated = dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["RatingExists"]) > 0;
                    btnRateClient.Enabled = (status == "completed" && !isRated);

                    bool hasRatingColumn = lvAssigned.SelectedItems[0].SubItems.Count > 7;
                    string rating = hasRatingColumn ? lvAssigned.SelectedItems[0].SubItems[7].Text : "Not rated";
                    btnShowFeedback.Enabled = hasRatingColumn && rating != "Not rated" && lvAssigned.SelectedItems[0].Tag != null && !string.IsNullOrEmpty(lvAssigned.SelectedItems[0].Tag?.ToString());
                }
                else
                {
                    btnComplete.Enabled = false;
                    btnInProgress.Enabled = false;
                    btnRateClient.Enabled = false;
                    btnShowFeedback.Enabled = false;
                }
            };

            btnShowFeedback.Click += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    string feedback = lvAssigned.SelectedItems[0].Tag?.ToString() ?? "No feedback available.";
                    MessageBox.Show(feedback, "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };


            btnComplete.Click += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    var selectedItem = lvAssigned.SelectedItems[0];
                    string taskName = selectedItem.SubItems[0].Text;
                    int requestId = int.Parse(selectedItem.SubItems[4].Text);

                    if (MessageBox.Show($"Are you sure you want to mark '{taskName}' as completed?",
                                        "Confirm Task Completion",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        bool success = _workerService.MarkTaskAsCompleted(requestId, _loggedInWorkerId.Value);

                        if (success)
                        {
                            LoadAssignedTasksWithRating(lvAssigned);
                            btnComplete.Enabled = false;
                            UpdateTotalMoney();
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
                            LoadAssignedTasksWithRating(lvAssigned);
                            btnInProgress.Enabled = false;
                            MessageBox.Show("Task marked as in progress.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to mark task as in progress. The task may already be in progress or completed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };


            btnRateClient.Click += (s, e) =>
            {
                if (lvAssigned.SelectedItems.Count > 0)
                {
                    var selectedItem = lvAssigned.SelectedItems[0];
                    int requestId = int.Parse(selectedItem.SubItems[4].Text);


                    Form rateForm = new Form
                    {
                        Text = "Rate Client",
                        Size = new System.Drawing.Size(400, 300),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    Label lblRating = new Label { Text = "Rating (1-5):", Location = new System.Drawing.Point(20, 20) };
                    NumericUpDown numRating = new NumericUpDown
                    {
                        Location = new System.Drawing.Point(150, 20),
                        Minimum = 1,
                        Maximum = 5,
                        Width = 50
                    };

                    Label lblFeedback = new Label { Text = "Feedback:", Location = new System.Drawing.Point(20, 60) };
                    TextBox txtFeedback = new TextBox
                    {
                        Location = new System.Drawing.Point(150, 60),
                        Width = 200,
                        Height = 100,
                        Multiline = true
                    };

                    Button btnSubmitRating = new Button
                    {
                        Text = "Submit",
                        Location = new System.Drawing.Point(150, 180),
                        Width = 100
                    };

                    btnSubmitRating.Click += (sender, args) =>
                    {
                        try
                        {
                            decimal ratingValue = numRating.Value;
                            string feedback = txtFeedback.Text;

                            _workerService.RateClient(requestId, _loggedInWorkerId.Value, ratingValue, feedback);

                            MessageBox.Show("Client rated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            rateForm.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error rating client: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };

                    rateForm.Controls.Add(lblRating);
                    rateForm.Controls.Add(numRating);
                    rateForm.Controls.Add(lblFeedback);
                    rateForm.Controls.Add(txtFeedback);
                    rateForm.Controls.Add(btnSubmitRating);

                    rateForm.ShowDialog();
                }
            };

            tab.Controls.Add(lblTitle);
            tab.Controls.Add(lvAssigned);
            tab.Controls.Add(lblTotalMoney);
            tab.Controls.Add(btnComplete);
            tab.Controls.Add(btnInProgress);
            tab.Controls.Add(btnRateClient);
            tab.Controls.Add(btnShowFeedback);
        }

        // load all assigned tasks while joining with task requests, then join with tasks to get specific details for each task
        //left join with worker ratings to get rating and feedback, so that even if it is not rated, it will still show the task
        private void LoadAssignedTasksWithRating(ListView lvAssigned)
        {
            lvAssigned.Items.Clear();
            int workerId = _loggedInWorkerId ?? 0;
            var dt = _databaseService.ExecuteQuery(@"
                SELECT t.TaskName, l.Area, t.AverageFee, tr.Status, tr.id as RequestID,
                       ta.StartedTime, ta.ActualTimeSlot, ta.ActualDurationMinutes, ta.Status as AssignmentStatus,
                       wr.RatingValue AS WorkerRating, wr.Feedback AS WorkerFeedback, tr.TaskID
                FROM TaskAssignments ta
                JOIN TaskRequests tr ON ta.RequestID = tr.id
                JOIN Tasks t ON tr.TaskID = t.id
                JOIN Locations l ON tr.LocationID = l.id
                LEFT JOIN WorkerRatings wr ON wr.WorkerID = ta.WorkerID AND wr.TaskID = tr.TaskID AND wr.RequestID = tr.id
                WHERE ta.WorkerID = @W AND tr.Status <> 'open'",
                cmd => cmd.Parameters.AddWithValue("@W", workerId));

            foreach (DataRow row in dt.Rows)
            {
                string fee = row["AverageFee"] == DBNull.Value ? "" : Convert.ToDecimal(row["AverageFee"]).ToString("F2");
                string startedTime = row["StartedTime"] == DBNull.Value ? "" : Convert.ToDateTime(row["StartedTime"]).ToString("g");
                string duration = string.Empty;
                if (!(row["ActualDurationMinutes"] is DBNull) && row["ActualDurationMinutes"] != null)
                {
                    duration = row["ActualDurationMinutes"].ToString() ?? string.Empty;
                }
                string status = row["AssignmentStatus"]?.ToString() ?? row["Status"].ToString() ?? "";
                string rating = (row["WorkerRating"] == DBNull.Value || row["WorkerRating"] == null) ? "Not rated" : row["WorkerRating"].ToString() ?? "Not rated";
                string feedback = row["WorkerFeedback"]?.ToString() ?? "";
                var item = new ListViewItem(new[]
                {
                    row["TaskName"].ToString() ?? "",
                    row["Area"].ToString() ?? "",
                    fee,
                    status,
                    row["RequestID"].ToString() ?? "",
                    startedTime,
                    duration,
                    rating
                })
                {
                    Tag = feedback
                };
                lvAssigned.Items.Add(item);
            }
        }
    }
}