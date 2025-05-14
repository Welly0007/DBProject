# Worker Management Application

A simple Windows Forms application to manage workers and tasks in a SQL Server database.

## Prerequisites

- **SQL Server** (Express or higher)
  - [Download SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
  - [Download SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads#SQLServer)

- **SQL Server Management Studio (SSMS)**
  - [Download SSMS](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

- **.NET SDK** (version 5.0 or higher)
  - [Download .NET SDK](https://dotnet.microsoft.com/download)

- **Visual Studio Code** or any preferred C# IDE
  - [Download VS Code](https://code.visualstudio.com/download)
  - Required Extensions for VS Code:
    - C# Dev Kit
    - .NET Core Tools

## Setup Instructions

### Step 1: Set Up the Database

1. Open SQL Server Management Studio (SSMS)
2. Connect to your SQL Server instance
3. Create a new database named `TaskWorkerDB`:
   ```sql
   CREATE DATABASE WorkerDB;
   GO
   ```
4. Run the database schema script:
   - Open the `initial-schema.sql` file provided with the project
   - Execute the script against the `TaskWorkerDB` database

### Step 2: Configure the Project

1. Extract/clone the project to your local machine
2. Open the project folder in Visual Studio Code or your preferred IDE
3. **Important**: Modify the connection string in `Form1.cs` to match your SQL Server:
   - Locate this line (around line 15):
     ```csharp
     private readonly string _connectionString = 
         "Server=WELLY-PC\\SQLEXPRESS;Database=TaskWorkerDB;Trusted_Connection=True;TrustServerCertificate=True;";
     ```
   - Change `WELLY-PC\\SQLEXPRESS` to your SQL Server instance name

### Step 3: Install Required Packages

1. Open a terminal/command prompt in the project directory
2. Run the following command to restore packages:
   ```
   dotnet restore
   ```
3. If needed, manually install the Microsoft.Data.SqlClient package:
   ```
   dotnet add package Microsoft.Data.SqlClient
   ```

### Step 4: Build and Run the Application

1. Build the project:
   ```
   dotnet build
   ```
2. Run the application:
   ```
   dotnet run
   ```

## Troubleshooting

### Connection Issues
- Verify your SQL Server instance name is correct
- Ensure SQL Server is running (check Services in Windows)
- If using Windows Authentication, make sure your Windows account has access to the database
- If firewall is blocking connections, allow SQL Server through the firewall

### Build Errors
- Make sure you have the correct .NET SDK version installed
- Try cleaning the solution: `dotnet clean` then `dotnet build`
- Verify all required packages are installed: `dotnet restore`

## Application Features

- **View Workers**: All workers are displayed in a grid
- **Add Worker**: Enter a name and click "Add Worker"
- **Update Worker**: Select a worker from the grid, edit the name, click "Update"
- **Delete Worker**: Select a worker and click "Delete"

## Need Help?

If you encounter any issues, please contact [Your Name/Email] for assistance.