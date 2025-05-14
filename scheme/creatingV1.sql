-- Create Worker table
CREATE TABLE Workers (
    id INT PRIMARY KEY IDENTITY(1,1),
    [Name] VARCHAR(100) NOT NULL
);

-- Create Specialty table
CREATE TABLE Specialties (
    id INT PRIMARY KEY IDENTITY(1,1),
    [Name] VARCHAR(100) NOT NULL
);

-- Create junction table for Worker-Specialty many-to-many relationship
CREATE TABLE WorkerSpecialty (
    WorkerID INT NOT NULL,
    SpecialtyID INT NOT NULL,
    PRIMARY KEY (WorkerID, SpecialtyID),
    FOREIGN KEY (WorkerID) REFERENCES Workers(id),
    FOREIGN KEY (SpecialtyID) REFERENCES Specialties(id)
);

-- Create TimeSlot table
CREATE TABLE TimeSlots (
    id INT PRIMARY KEY IDENTITY(1,1),
    DayOfWeek TINYINT NOT NULL, -- 1=Monday, 2=Tuesday, etc.
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    CONSTRAINT CHK_DayOfWeek CHECK (DayOfWeek BETWEEN 1 AND 7),
    CONSTRAINT CHK_TimeOrder CHECK (StartTime < EndTime),
    CONSTRAINT CHK_BusinessHours CHECK (StartTime >= '09:00:00' AND EndTime <= '17:00:00')
);

-- Create Location table
CREATE TABLE Locations (
    id INT PRIMARY KEY IDENTITY(1,1),
    Area NVARCHAR(100) NOT NULL
);

-- Create junction table for Worker-TimeSlot-Location (availability)
CREATE TABLE WorkerAvailability (
    WorkerID INT NOT NULL,
    TimeSlotID INT NOT NULL,
    LocationID INT NOT NULL,
    SpecialtyID INT NOT NULL,
    PRIMARY KEY (WorkerID, TimeSlotID, LocationID),
    FOREIGN KEY (WorkerID) REFERENCES Workers(id),
    FOREIGN KEY (TimeSlotID) REFERENCES TimeSlots(id),
    FOREIGN KEY (LocationID) REFERENCES Locations(id),
    FOREIGN KEY (SpecialtyID) REFERENCES Specialties(id)
);

-- Create Client table
CREATE TABLE Clients (
    id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Address VARCHAR(200),
    PaymentInfo VARCHAR(255)
);

-- Create Task table
CREATE TABLE Tasks (
    id INT PRIMARY KEY IDENTITY(1,1),
    TaskName VARCHAR(100) NOT NULL,
    AverageDuration INT,
    AverageFee DECIMAL(10,2),
    SpecialtyID INT,
    FOREIGN KEY (SpecialtyID) REFERENCES Specialties(id)
);

-- Create TaskRequest table
CREATE TABLE TaskRequests (
    id INT PRIMARY KEY IDENTITY(1,1),
    ClientID INT NOT NULL,
    TaskID INT NOT NULL,
    RequestedDateTime DATETIME NOT NULL,
    PreferredTimeSlot VARCHAR(50),
    RequestAddress VARCHAR(200),
    FOREIGN KEY (ClientID) REFERENCES Clients(id),
    FOREIGN KEY (TaskID) REFERENCES Tasks(id)
);

-- Create TaskAssignment table
CREATE TABLE TaskAssignments (
    id INT PRIMARY KEY IDENTITY(1,1),
    WorkerID INT NOT NULL,
    RequestID INT NOT NULL,
    ActualTimeSlot DATETIME,
    ActualDurationMinutes INT,
    WorkerRating DECIMAL(3,2),
    ClientRating DECIMAL(3,2),
    FOREIGN KEY (WorkerID) REFERENCES Workers(id),
    FOREIGN KEY (RequestID) REFERENCES TaskRequests(id)
);

-- Create WorkerRating table
CREATE TABLE WorkerRatings (
    id INT PRIMARY KEY IDENTITY(1,1),
    WorkerID INT NOT NULL,
    TaskID INT NOT NULL,
    RatingValue DECIMAL(3,2),
    Date DATE,
    Feedback NVARCHAR(MAX),
    FOREIGN KEY (WorkerID) REFERENCES Workers(id),
    FOREIGN KEY (TaskID) REFERENCES Tasks(id)
);

-- Create ClientRating table
CREATE TABLE ClientRatings (
    id INT PRIMARY KEY IDENTITY(1,1),
    ClientID INT NOT NULL,
    TaskID INT NOT NULL,
    RatingValue DECIMAL(3,2),
    Date DATE,
    Feedback NVARCHAR(MAX),
    FOREIGN KEY (ClientID) REFERENCES Clients(id),
    FOREIGN KEY (TaskID) REFERENCES Tasks(id)
);