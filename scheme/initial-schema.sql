-- Drop all tables (in order to avoid FK constraint errors)
DROP TABLE IF EXISTS WorkerRatings;
DROP TABLE IF EXISTS ClientRatings;
DROP TABLE IF EXISTS TaskAssignments;
DROP TABLE IF EXISTS TaskRequests;
DROP TABLE IF EXISTS WorkerAvailability;
DROP TABLE IF EXISTS WorkerSpecialty;
DROP TABLE IF EXISTS Tasks;
DROP TABLE IF EXISTS Locations;
DROP TABLE IF EXISTS TimeSlots;
DROP TABLE IF EXISTS Specialties;
DROP TABLE IF EXISTS Workers;
DROP TABLE IF EXISTS Clients;

-- Create Worker table
CREATE TABLE Workers (
    id INT PRIMARY KEY IDENTITY(1,1),
    [Name] VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100)
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
    PRIMARY KEY (WorkerID, TimeSlotID, LocationID, SpecialtyID),
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
    PreferredTimeSlot DATETIME,
    RequestAddress VARCHAR(200),
    Status VARCHAR(20) NOT NULL DEFAULT 'open',
    FOREIGN KEY (ClientID) REFERENCES Clients(id),
    FOREIGN KEY (TaskID) REFERENCES Tasks(id),
    CONSTRAINT CHK_RequestStatus CHECK (Status IN ('open', 'assigned', 'completed', 'cancelled'))
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
    Status VARCHAR(20) NOT NULL DEFAULT 'scheduled',
    FOREIGN KEY (WorkerID) REFERENCES Workers(id),
    FOREIGN KEY (RequestID) REFERENCES TaskRequests(id),
    CONSTRAINT CHK_AssignmentStatus CHECK (Status IN ('scheduled', 'in_progress', 'completed', 'cancelled'))
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


-- Dummy data for Specialties
INSERT INTO Specialties ([Name]) VALUES ('Plumbing');
INSERT INTO Specialties ([Name]) VALUES ('Electrical');
INSERT INTO Specialties ([Name]) VALUES ('Cleaning');
INSERT INTO Specialties ([Name]) VALUES ('Painting');

-- Dummy data for Locations
INSERT INTO Locations (Area) VALUES (N'Downtown');
INSERT INTO Locations (Area) VALUES (N'Suburbs');
INSERT INTO Locations (Area) VALUES (N'Industrial Zone');
INSERT INTO Locations (Area) VALUES (N'University District');

-- Dummy data for TimeSlots
INSERT INTO TimeSlots (DayOfWeek, StartTime, EndTime) VALUES (1, '09:00:00', '12:00:00'); -- Monday Morning
INSERT INTO TimeSlots (DayOfWeek, StartTime, EndTime) VALUES (1, '13:00:00', '17:00:00'); -- Monday Afternoon
INSERT INTO TimeSlots (DayOfWeek, StartTime, EndTime) VALUES (2, '09:00:00', '12:00:00'); -- Tuesday Morning
INSERT INTO TimeSlots (DayOfWeek, StartTime, EndTime) VALUES (2, '13:00:00', '17:00:00'); -- Tuesday Afternoon

-- Dummy data for Tasks
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Fix Leaky Faucet', 60, 50.00, 2);
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Install Light Fixture', 45, 40.00, 2);
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Deep Clean Apartment', 120, 100.00, 3);
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Paint Living Room', 180, 200.00, 4);

-- Default test client and worker
INSERT INTO Clients (Name, Phone, Email, Address, PaymentInfo)
VALUES ('Test Client', '1234567890', 'client@test.com', '123 Test St', 'VISA 1111');

INSERT INTO Workers (Name, Phone, Email)
VALUES ('Test Worker', '0987654321', 'worker@test.com');

-- Connect test worker with specialties, locations, and time slots
-- Assume: Plumbing = 1, Electrical = 2, Cleaning = 3, Painting = 4
--         Downtown = 1, Suburbs = 2, Industrial Zone = 3, University District = 4
--         TimeSlot IDs: 1, 2, 3, 4

-- Assign specialties (Plumbing and Electrical) to test worker
INSERT INTO WorkerSpecialty (WorkerID, SpecialtyID) VALUES (1, 1);
INSERT INTO WorkerSpecialty (WorkerID, SpecialtyID) VALUES (1, 2);

-- Assign availability for test worker: all combinations of specialties, locations, and time slots
INSERT INTO WorkerAvailability (WorkerID, TimeSlotID, LocationID, SpecialtyID) VALUES (1, 1, 1, 1);
INSERT INTO WorkerAvailability (WorkerID, TimeSlotID, LocationID, SpecialtyID) VALUES (1, 2, 1, 1);
INSERT INTO WorkerAvailability (WorkerID, TimeSlotID, LocationID, SpecialtyID) VALUES (1, 1, 2, 2);
INSERT INTO WorkerAvailability (WorkerID, TimeSlotID, LocationID, SpecialtyID) VALUES (1, 2, 2, 2);
-- Add more as needed for your test coverage