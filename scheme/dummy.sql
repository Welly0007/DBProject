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
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Fix Leaky Faucet', 60, 50.00, 1);
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Install Light Fixture', 45, 40.00, 2);
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Deep Clean Apartment', 120, 100.00, 3);
INSERT INTO Tasks (TaskName, AverageDuration, AverageFee, SpecialtyID) VALUES ('Paint Living Room', 180, 200.00, 4);