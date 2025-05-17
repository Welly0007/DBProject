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

        public Form1()
        {
            InitializeComponent();
            string schemaFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scheme", "initial-schema.sql");
            _databaseService = new DatabaseService(
                "Server=WELLY-PC\\SQLEXPRESS;Database=TaskWorkerDB;Trusted_Connection=True;TrustServerCertificate=True;",
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
            Button btnResetDb = new Button { 
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
                }
            }
            else
            {
                tabWorkerProfile = new TabPage("Worker Profile");
                SetupWorkerProfileTab(tabWorkerProfile);
                tabControl.TabPages.Add(tabWorkerProfile);
            }

            this.Controls.Add(tabControl);
        }

        private void AddBackToMenuButton(TabPage tab)
        {
            Button btnBack = new Button { Text = "Back to Main Menu", Location = new System.Drawing.Point(10, 10), Width = 150 };
            btnBack.Click += (s, e) =>
            {
                this.Controls.Remove(tabControl!);
                tabControl = null; // Reset tabControl to ensure proper reinitialization
                this.Controls.Add(pnlMainMenu!);
            };
            tab.Controls.Add(btnBack);
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

            // Worker selection or creation
            Label lblSelect = new Label { Text = "Select Worker:", Location = new System.Drawing.Point(30, 30 + yOffset) };
            ComboBox cmbWorkers = new ComboBox { Location = new System.Drawing.Point(150, 30 + yOffset), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnLoad = new Button { Text = "Enter Profile", Location = new System.Drawing.Point(370, 30 + yOffset), Width = 120 };
            Button btnNew = new Button { Text = "Create Profile", Location = new System.Drawing.Point(500, 30 + yOffset), Width = 120 };

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

            // Populate workers
            cmbWorkers.Items.Clear();
            foreach (DataRow row in _workerService.GetAllWorkers().Rows)
                cmbWorkers.Items.Add(new ComboBoxItem(row["Name"]?.ToString() ?? "", (int)row["Id"]));

            // Initially, only allow creating a new profile
            txtName.Enabled = true;
            txtPhone.Enabled = true;
            txtEmail.Enabled = true;
            clbSpecialties.Enabled = false;
            clbLocations.Enabled = false;
            clbTimeSlots.Enabled = false;
            btnSave.Enabled = false;

            btnLoad.Click += (s, e) =>
            {
                if (cmbWorkers.SelectedItem is ComboBoxItem workerItem)
                {
                    var profile = _workerService.GetWorkerProfile(workerItem.Value);
                    txtName.Text = profile.Name;
                    txtPhone.Text = profile.Phone;
                    txtEmail.Text = profile.Email;

                    // Disable editing of basic info
                    txtName.Enabled = false;
                    txtPhone.Enabled = false;
                    txtEmail.Enabled = false;

                    // Enable specialties, locations, timeslots
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
            };

            btnNew.Click += (s, e) =>
            {
                cmbWorkers.SelectedIndex = -1;
                txtName.Text = "";
                txtPhone.Text = "";
                txtEmail.Text = "";
                for (int i = 0; i < clbSpecialties.Items.Count; i++) clbSpecialties.SetItemChecked(i, false);
                for (int i = 0; i < clbLocations.Items.Count; i++) clbLocations.SetItemChecked(i, false);
                for (int i = 0; i < clbTimeSlots.Items.Count; i++) clbTimeSlots.SetItemChecked(i, false);

                txtName.Enabled = true;
                txtPhone.Enabled = true;
                txtEmail.Enabled = true;
                clbSpecialties.Enabled = false;
                clbLocations.Enabled = false;
                clbTimeSlots.Enabled = false;
                btnSave.Enabled = true;
            };

            btnSave.Click += (s, e) =>
            {
                var specialtyIds = clbSpecialties.CheckedItems.OfType<ComboBoxItem>().Select(x => x.Value).ToList();
                var locationIds = clbLocations.CheckedItems.OfType<ComboBoxItem>().Select(x => x.Value).ToList();
                var timeSlotIds = clbTimeSlots.CheckedItems.OfType<ComboBoxItem>().Select(x => x.Value).ToList();

                int? workerId = (cmbWorkers.SelectedItem is ComboBoxItem workerItem) ? workerItem.Value : (int?)null;

                // If creating new profile, save and reload worker list, then switch to edit mode
                if (workerId == null)
                {
                    _workerService.SaveOrUpdateProfile(null, txtName.Text, txtPhone.Text, txtEmail.Text, specialtyIds, locationIds, timeSlotIds);
                    MessageBox.Show("Profile created. Now select yourself from the list to edit specialties, locations, and timeslots.");
                    // Refresh worker list
                    cmbWorkers.Items.Clear();
                    foreach (DataRow row in _workerService.GetAllWorkers().Rows)
                        cmbWorkers.Items.Add(new ComboBoxItem(row["Name"]?.ToString() ?? "", (int)row["Id"]));
                    txtName.Text = "";
                    txtPhone.Text = "";
                    txtEmail.Text = "";
                    for (int i = 0; i < clbSpecialties.Items.Count; i++) clbSpecialties.SetItemChecked(i, false);
                    for (int i = 0; i < clbLocations.Items.Count; i++) clbLocations.SetItemChecked(i, false);
                    for (int i = 0; i < clbTimeSlots.Items.Count; i++) clbTimeSlots.SetItemChecked(i, false);
                    txtName.Enabled = true;
                    txtPhone.Enabled = true;
                    txtEmail.Enabled = true;
                    clbSpecialties.Enabled = false;
                    clbLocations.Enabled = false;
                    clbTimeSlots.Enabled = false;
                    btnSave.Enabled = false;
                }
                else
                {
                    _workerService.SaveOrUpdateProfile(workerId, txtName.Text, txtPhone.Text, txtEmail.Text, specialtyIds, locationIds, timeSlotIds);
                    MessageBox.Show("Profile updated.");
                }
            };

            tab.Controls.Add(lblSelect); tab.Controls.Add(cmbWorkers); tab.Controls.Add(btnLoad); tab.Controls.Add(btnNew);
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
            Label lblTaskInfo = new Label { 
                Location = new System.Drawing.Point(140, 220 + yOffset),
                Width = 600,
                Height = 100,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            // List view for showing all tasks
            ListView lvTasks = new ListView {
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
                if (lvTasks.SelectedItems.Count > 0)
                {
                    int taskId = (int)lvTasks.SelectedItems[0].Tag;
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
                Size = new System.Drawing.Size(400, 350),
                StartPosition = FormStartPosition.CenterParent
            };

            Label lblAddress = new Label { Text = "Request Address:", Location = new System.Drawing.Point(20, 20) };
            TextBox txtAddress = new TextBox { Location = new System.Drawing.Point(150, 20), Width = 200 };

            // Add location selection
            Label lblLocation = new Label { Text = "Location Area:", Location = new System.Drawing.Point(20, 60) };
            ComboBox cmbLocations = new ComboBox { 
                Location = new System.Drawing.Point(150, 60), 
                Width = 200, 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            
            // Load available locations
            DataTable locations = _workerService.GetAllLocations();
            foreach (DataRow row in locations.Rows)
            {
                cmbLocations.Items.Add(new ComboBoxItem(row["Area"].ToString() ?? "", Convert.ToInt32(row["Id"])));
            }
            
            // Select first location by default if available
            if (cmbLocations.Items.Count > 0)
                cmbLocations.SelectedIndex = 0;

            Label lblPreferredTime = new Label { Text = "Preferred Time:", Location = new System.Drawing.Point(20, 100) };
            DateTimePicker dtpPreferredTime = new DateTimePicker { Location = new System.Drawing.Point(150, 100), Width = 200 };

            Button btnSubmit = new Button { Text = "Submit Request", Location = new System.Drawing.Point(150, 140), Width = 150 };

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

                try
                {
                    int locationId = ((ComboBoxItem)cmbLocations.SelectedItem).Value;
                    _taskCatalogService.CreateTaskRequest(
                        taskId, 
                        _loggedInClientId.Value, 
                        txtAddress.Text, 
                        dtpPreferredTime.Value, 
                        locationId);
                    MessageBox.Show("Task request submitted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            requestForm.Controls.Add(lblPreferredTime);
            requestForm.Controls.Add(dtpPreferredTime);
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
    }
}