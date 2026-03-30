--Event 1 – Tech Conference
--INSERT INTO Events (Id, Name, Description, Venue, Date, Time, TotalCapacity, AvailableTickets, CreatedAt, UpdatedAt, Status)
--VALUES 
--('11111111-1111-1111-1111-111111111111', 'Global Tech Conference', 'Annual technology conference', 'London Expo Center', '2026-06-15', '09:00:00', 1000, 1000, DATE('now'), DATE('now'), 1);

--INSERT INTO PricingTiers (Id, EventId, Name, Price, Capacity, AvailableTickets, MinQuantityPerOrder, MaxQuantityPerOrder, TierType, SaleEndDate)
--VALUES
--('11111111-aaaa-aaaa-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'General', 100, 700, 700, 1, 10, 1, NULL),
--('11111111-bbbb-bbbb-bbbb-111111111111', '11111111-1111-1111-1111-111111111111', 'VIP', 250, 200, 200, 1, 5, 2, NULL),
--('11111111-cccc-cccc-cccc-111111111111', '11111111-1111-1111-1111-111111111111', 'Early Bird', 80, 100, 100, 1, 8, 3, '2026-05-30');

--Event 2 – Music Festival
INSERT INTO Events  (Id, Name, Description, Venue, Date, Time, TotalCapacity, AvailableTickets, CreatedAt, UpdatedAt, Status)
VALUES 
('22222222-2222-2222-2222-222222222222', 'Summer Music Festival', 'Outdoor live music festival', 'Hyde Park', '2026-07-10', '15:00:00', 1500, 1500, DATE('now'), DATE('now'), 1);

INSERT INTO PricingTiers  (Id, Name, Description, Venue, Date, Time, TotalCapacity, AvailableTickets, CreatedAt, UpdatedAt, Status)
VALUES
('22222222-aaaa-aaaa-aaaa-222222222222', '22222222-2222-2222-2222-222222222222', 'General', 70, 1000, 1000, 1, 15, 1, '2026-07-09'),
('22222222-bbbb-bbbb-bbbb-222222222222', '22222222-2222-2222-2222-222222222222', 'VIP', 180, 300, 300, 1, 6, 2, '2026-07-09'),
('22222222-cccc-cccc-cccc-222222222222', '22222222-2222-2222-2222-222222222222', 'Early Bird', 50, 200, 200, 1, 10, 3, '2026-06-20');

--Event 3 – Startup Meetup
INSERT INTO Events  (Id, Name, Description, Venue, Date, Time, TotalCapacity, AvailableTickets, CreatedAt, UpdatedAt, Status)
VALUES 
('33333333-3333-3333-3333-333333333333', 'Startup Networking Meetup', 'Meet founders and investors', 'Berlin Hub', '2026-05-20', '18:00:00', 300, 300, DATE('now'), DATE('now'), 1);

INSERT INTO PricingTiers VALUES
('33333333-aaaa-aaaa-aaaa-333333333333', '33333333-3333-3333-3333-333333333333', 'General', 40, 200, 200, 1, 5, 1, '2026-05-19'),
('33333333-bbbb-bbbb-bbbb-333333333333', '33333333-3333-3333-3333-333333333333', 'VIP', 100, 50, 50, 1, 3, 2, '2026-05-19'),
('33333333-cccc-cccc-cccc-333333333333', '33333333-3333-3333-3333-333333333333', 'Early Bird', 25, 50, 50, 1, 5, 3, '2026-05-10');

--Event 4 – Food Carnival
INSERT INTO Events  (Id, Name, Description, Venue, Date, Time, TotalCapacity, AvailableTickets, CreatedAt, UpdatedAt, Status)
VALUES 
('44444444-4444-4444-4444-444444444444', 'International Food Carnival', 'Global cuisines and food stalls', 'Dubai Marina', '2026-08-05', '12:00:00', 800, 800, DATE('now'), DATE('now'), 1);

INSERT INTO PricingTiers VALUES
('44444444-aaaa-aaaa-aaaa-444444444444', '44444444-4444-4444-4444-444444444444', 'General', 30, 500, 500, 1, 10, 1, '2026-08-04'),
('44444444-bbbb-bbbb-bbbb-444444444444', '44444444-4444-4444-4444-444444444444', 'VIP', 90, 200, 200, 1, 4, 2, '2026-08-04'),
('44444444-cccc-cccc-cccc-444444444444', '44444444-4444-4444-4444-444444444444', 'Early Bird', 20, 100, 100, 1, 8, 3, '2026-07-20');

--Event 5 – AI Workshop
INSERT INTO Events  (Id, Name, Description, Venue, Date, Time, TotalCapacity, AvailableTickets, CreatedAt, UpdatedAt, Status)
VALUES 
('55555555-5555-5555-5555-555555555555', 'AI Hands-on Workshop', 'Practical AI and ML sessions', 'San Francisco Lab', '2026-09-01', '10:00:00', 200, 200, DATE('now'), DATE('now'), 1);

INSERT INTO PricingTiers VALUES
('55555555-aaaa-aaaa-aaaa-555555555555', '55555555-5555-5555-5555-555555555555', 'General', 120, 120, 120, 1, 5, 1, '2026-08-31'),
('55555555-bbbb-bbbb-bbbb-555555555555', '55555555-5555-5555-5555-555555555555', 'VIP', 250, 50, 50, 1, 2, 2, '2026-08-31'),
('55555555-cccc-cccc-cccc-555555555555', '55555555-5555-5555-5555-555555555555', 'Early Bird', 90, 30, 30, 1, 5, 3, '2026-08-15');
