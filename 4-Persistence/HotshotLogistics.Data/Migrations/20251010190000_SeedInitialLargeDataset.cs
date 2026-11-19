#pragma warning disable SA1602
#pragma warning disable SA1649
using System.Data;
using Bogus;

namespace HotshotLogistics.Data.Migrations;

public enum DriverStatus
{
    Available = 0,
    Busy = 1,
    OffDuty = 2,
    Maintenance = 3,
}

public enum JobStatus
{
    Pending = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}

public enum JobPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4,
}

public enum CreditStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
}

[Migration(20251010190000)]
public class SeedInitialLargeDataset : Migration
{
    public override void Up()
    {
        // This migration seeds the database with realistic test data using Bogus library
        // Creates 250 drivers and 300 jobs with geographic diversity across US states
        Execute.WithConnection((connection, transaction) =>
        {
            var random = new Random(12345); // deterministic randomness

            // US States for geographic diversity
            var usStates = new[]
            {
                new StateInfo("AL", "Alabama", 32.3777m, -86.3006m),
                new StateInfo("AK", "Alaska", 61.3707m, -152.4044m),
                new StateInfo("AZ", "Arizona", 33.4484m, -112.0740m),
                new StateInfo("AR", "Arkansas", 34.7465m, -92.2896m),
                new StateInfo("CA", "California", 36.7783m, -119.4179m),
                new StateInfo("CO", "Colorado", 39.7392m, -104.9903m),
                new StateInfo("CT", "Connecticut", 41.7658m, -72.6734m),
                new StateInfo("DE", "Delaware", 39.1453m, -75.4189m),
                new StateInfo("FL", "Florida", 27.7663m, -81.6868m),
                new StateInfo("GA", "Georgia", 32.1656m, -82.9001m),
                new StateInfo("HI", "Hawaii", 21.0943m, -157.4983m),
                new StateInfo("ID", "Idaho", 44.0682m, -114.7420m),
                new StateInfo("IL", "Illinois", 40.6331m, -89.3985m),
                new StateInfo("IN", "Indiana", 39.7684m, -86.1581m),
                new StateInfo("IA", "Iowa", 41.8780m, -93.0977m),
                new StateInfo("KS", "Kansas", 39.0119m, -98.4842m),
                new StateInfo("KY", "Kentucky", 37.8393m, -84.2700m),
                new StateInfo("LA", "Louisiana", 30.9843m, -91.9623m),
                new StateInfo("ME", "Maine", 45.2538m, -69.0064m),
                new StateInfo("MD", "Maryland", 39.0458m, -76.6413m),
                new StateInfo("MA", "Massachusetts", 42.3601m, -71.0589m),
                new StateInfo("MI", "Michigan", 44.3148m, -85.6024m),
                new StateInfo("MN", "Minnesota", 46.7296m, -94.6859m),
                new StateInfo("MS", "Mississippi", 32.3547m, -89.3985m),
                new StateInfo("MO", "Missouri", 37.9643m, -91.8318m),
                new StateInfo("MT", "Montana", 46.8797m, -110.3626m),
                new StateInfo("NE", "Nebraska", 41.4925m, -99.9018m),
                new StateInfo("NV", "Nevada", 38.8026m, -116.4194m),
                new StateInfo("NH", "New Hampshire", 43.1939m, -71.5724m),
                new StateInfo("NJ", "New Jersey", 40.0583m, -74.4057m),
                new StateInfo("NM", "New Mexico", 34.9727m, -105.0324m),
                new StateInfo("NY", "New York", 40.7128m, -74.0060m),
                new StateInfo("NC", "North Carolina", 35.7596m, -79.0193m),
                new StateInfo("ND", "North Dakota", 47.5515m, -101.0020m),
                new StateInfo("OH", "Ohio", 40.3675m, -82.9962m),
                new StateInfo("OK", "Oklahoma", 35.4671m, -97.5164m),
                new StateInfo("OR", "Oregon", 43.8041m, -120.5542m),
                new StateInfo("PA", "Pennsylvania", 41.2033m, -77.1945m),
                new StateInfo("RI", "Rhode Island", 41.5801m, -71.4774m),
                new StateInfo("SC", "South Carolina", 33.8361m, -81.1637m),
                new StateInfo("SD", "South Dakota", 43.9695m, -99.9018m),
                new StateInfo("TN", "Tennessee", 36.1627m, -86.7816m),
                new StateInfo("TX", "Texas", 31.9686m, -99.9018m),
                new StateInfo("UT", "Utah", 40.7608m, -111.8910m),
                new StateInfo("VT", "Vermont", 44.2601m, -72.5778m),
                new StateInfo("VA", "Virginia", 37.4316m, -78.6569m),
                new StateInfo("WA", "Washington", 47.6062m, -122.3321m),
                new StateInfo("WV", "West Virginia", 38.5976m, -80.4549m),
                new StateInfo("WI", "Wisconsin", 43.7844m, -88.7879m),
                new StateInfo("WY", "Wyoming", 43.0759m, -107.2903m),
            };

            // Generate 250 realistic drivers
            var driverFaker = new Faker<DriverData>()
                .RuleFor(d => d.FirstName, f => f.Name.FirstName())
                .RuleFor(d => d.LastName, f => f.Name.LastName())
                .RuleFor(d => d.Email, (f, d) => $"{d.FirstName.ToLower()}.{d.LastName.ToLower()}{f.Random.Number(1, 999)}@drivers.logistics.com")
                .RuleFor(d => d.PhoneNumber, f => f.Phone.PhoneNumber("(###) ###-####"))
                .RuleFor(d => d.LicenseNumber, f => $"DL{f.Random.Replace("#########")}")
                .RuleFor(d => d.LicenseState, f => f.PickRandom(usStates.Select(s => s.Code).ToArray()))
                .RuleFor(d => d.LicenseExpiryDate, f => f.Date.Future(2, DateTime.UtcNow.AddYears(1)))
                .RuleFor(d => d.InsuranceExpiryDate, f => f.Date.Future(1, DateTime.UtcNow.AddMonths(6)))
                .RuleFor(d => d.CurrentStatus, f => f.PickRandom<DriverStatus>())
                .RuleFor(d => d.IsActive, f => f.Random.Bool(0.9f)) // 90% active
                .RuleFor(d => d.VehicleInfo, f => new VehicleInfo
                {
                    Make = f.Vehicle.Manufacturer(),
                    Model = f.Vehicle.Model(),
                    Year = f.Random.Number(2015, 2024),
                    LicensePlate = f.Random.Replace("###-####"),
                    VIN = f.Random.Replace("#################"),
                    Type = f.PickRandom("Pickup Truck", "Box Truck", "Semi Truck", "Van", "Flatbed Truck"),
                    Capacity = f.PickRandom("5 tons", "10 tons", "15 tons", "20 tons", "25 tons"),
                });

            var drivers = driverFaker.Generate(250);

            // Insert drivers
            foreach (var driver in drivers)
            {
                // Check if driver already exists
                using var checkDriver = connection.CreateCommand();
                checkDriver.Transaction = transaction;
                checkDriver.CommandText = "SELECT COUNT(1) FROM Drivers WHERE Email = @Email";
                checkDriver.Parameters.Add(CreateParam(checkDriver, "@Email", driver.Email));
                var existsDriver = Convert.ToInt32(checkDriver.ExecuteScalar() ?? 0) > 0;
                if (existsDriver)
                {
                    continue;
                }

                using var insertDriver = connection.CreateCommand();
                insertDriver.Transaction = transaction;
                insertDriver.CommandText = @"
                INSERT INTO Drivers (FirstName, LastName, Email, PhoneNumber, LicenseNumber, LicenseState, LicenseExpiryDate, InsuranceExpiryDate, CurrentStatus, IsActive, CreatedAt)
                VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @LicenseNumber, @LicenseState, @LicenseExpiryDate, @InsuranceExpiryDate, @CurrentStatus, @IsActive, @CreatedAt)";

                insertDriver.Parameters.Add(CreateParam(insertDriver, "@FirstName", driver.FirstName));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LastName", driver.LastName));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@Email", driver.Email));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@PhoneNumber", driver.PhoneNumber));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LicenseNumber", driver.LicenseNumber));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LicenseState", driver.LicenseState));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LicenseExpiryDate", driver.LicenseExpiryDate));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@InsuranceExpiryDate", driver.InsuranceExpiryDate));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@CurrentStatus", (int)driver.CurrentStatus));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@IsActive", driver.IsActive));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@CreatedAt", DateTime.UtcNow));

                insertDriver.ExecuteNonQuery();
            }

            // Get driver IDs for job assignments
            var driverIds = new List<int>();
            using (var readDrivers = connection.CreateCommand())
            {
                readDrivers.Transaction = transaction;
                readDrivers.CommandText = "SELECT Id FROM Drivers WHERE Email LIKE '%@drivers.logistics.com'";
                using var reader = readDrivers.ExecuteReader();
                while (reader.Read())
                {
                    driverIds.Add(reader.GetInt32(0));
                }
            }

            // Generate 300 realistic jobs with geographic diversity
            var jobFaker = new Faker<JobData>()
                .RuleFor(j => j.Title, f => f.Company.CatchPhrase())
                .RuleFor(j => j.Status, f => f.PickRandom(JobStatus.Pending, JobStatus.Assigned, JobStatus.InProgress, JobStatus.Completed))
                .RuleFor(j => j.Priority, f => f.PickRandom(JobPriority.Low, JobPriority.Normal, JobPriority.High, JobPriority.Urgent))
                .RuleFor(j => j.BaseRate, f => Math.Round(f.Random.Decimal(100m, 1000m), 2))
                .RuleFor(j => j.ScheduledPickupTime, f => f.Date.Between(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(30)))
                .RuleFor(j => j.EstimatedDeliveryTime, (f, j) => j.ScheduledPickupTime.AddHours(f.Random.Number(4, 72)))
                .RuleFor(j => j.CargoDetails, f => new CargoDetails
                {
                    Description = f.Commerce.ProductDescription(),
                    Weight = f.Random.Decimal(500, 25000), // lbs
                    Dimensions = $"{f.Random.Number(2, 20)}ft x {f.Random.Number(2, 10)}ft x {f.Random.Number(2, 8)}ft",
                    Value = f.Random.Decimal(1000, 50000),
                    IsHazardous = f.Random.Bool(0.1f), // 10% hazardous
                });

            var jobs = jobFaker.Generate(300);

            // Generate customers for jobs
            var customerFaker = new Faker<CustomerData>()
                .RuleFor(c => c.CompanyName, f => f.Company.CompanyName())
                .RuleFor(c => c.TaxId, f => $"TAX{f.Random.Replace("#########")}")
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber("(###) ###-####"))
                .RuleFor(c => c.BillingAddress, f => f.Address.StreetAddress())
                .RuleFor(c => c.City, f => f.Address.City())
                .RuleFor(c => c.State, f => f.PickRandom(usStates.Select(s => s.Code).ToArray()))
                .RuleFor(c => c.ZipCode, f => f.Address.ZipCode("#####"))
                .RuleFor(c => c.Country, "USA")
                .RuleFor(c => c.CreditLimit, f => f.Random.Decimal(5000, 100000))
                .RuleFor(c => c.PaymentTermsDays, f => f.Random.Number(15, 60))
                .RuleFor(c => c.CreditStatus, f => f.PickRandom(CreditStatus.Approved, CreditStatus.Pending, CreditStatus.Denied))
                .RuleFor(c => c.IsActive, f => f.Random.Bool(0.95f));

            var customers = customerFaker.Generate(50);

            // Insert customers first
            foreach (var customer in customers)
            {
                var stateInfo = usStates.FirstOrDefault(s => s.Code == customer.State);
                var latitude = stateInfo != default ? stateInfo.Latitude + ((decimal)(random.NextDouble() - 0.5) * 2m) : 40.0m;
                var longitude = stateInfo != default ? stateInfo.Longitude + ((decimal)(random.NextDouble() - 0.5) * 2m) : -100.0m;

                using var checkCustomer = connection.CreateCommand();
                checkCustomer.Transaction = transaction;
                checkCustomer.CommandText = "SELECT COUNT(1) FROM Customers WHERE Email = @Email";
                checkCustomer.Parameters.Add(CreateParam(checkCustomer, "@Email", customer.Email));
                var existsCustomer = Convert.ToInt32(checkCustomer.ExecuteScalar() ?? 0) > 0;
                if (existsCustomer)
                {
                    continue;
                }

                using var insertCustomer = connection.CreateCommand();
                insertCustomer.Transaction = transaction;
                insertCustomer.CommandText = @"
                INSERT INTO Customers (Id, CompanyName, TaxId, Email, Phone, BillingAddress, City, State, ZipCode, Country, Latitude, Longitude, CreditLimit, PaymentTermsDays, CreditStatus, IsActive, CreatedAt)
                VALUES (@Id, @CompanyName, @TaxId, @Email, @Phone, @BillingAddress, @City, @State, @ZipCode, @Country, @Latitude, @Longitude, @CreditLimit, @PaymentTermsDays, @CreditStatus, @IsActive, @CreatedAt)";

                var companyNameClean = customer.CompanyName.Replace(" ", string.Empty).Replace("-", string.Empty).Replace(".", string.Empty);
                var nameLength = Math.Min(10, companyNameClean.Length);
                var customerId = $"CUST-{companyNameClean.Substring(0, Math.Max(3, nameLength))}-{random.Next(1000, 9999)}";

                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@Id", customerId));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@CompanyName", customer.CompanyName));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@TaxId", customer.TaxId));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@Email", customer.Email));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@Phone", customer.Phone));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@BillingAddress", customer.BillingAddress));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@City", customer.City));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@State", customer.State));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@ZipCode", customer.ZipCode));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@Country", customer.Country));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@Latitude", latitude));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@Longitude", longitude));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@CreditLimit", customer.CreditLimit));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@PaymentTermsDays", customer.PaymentTermsDays));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@CreditStatus", (int)customer.CreditStatus));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@IsActive", customer.IsActive));
                insertCustomer.Parameters.Add(CreateParam(insertCustomer, "@CreatedAt", DateTime.UtcNow));

                insertCustomer.ExecuteNonQuery();
            }

            // Get customer IDs for jobs
            var customerIds = new List<string>();
            using (var readCustomers = connection.CreateCommand())
            {
                readCustomers.Transaction = transaction;
                readCustomers.CommandText = "SELECT Id FROM Customers WHERE Email LIKE '%@%'";
                using var reader = readCustomers.ExecuteReader();
                while (reader.Read())
                {
                    customerIds.Add(reader.GetString(0));
                }
            }

            // Insert jobs with geographic diversity
            foreach (var job in jobs)
            {
                var customerId = customerIds[random.Next(customerIds.Count)];
                var assignedDriverId = driverIds[random.Next(driverIds.Count)];

                // Generate pickup and delivery locations in different states for geographic diversity
                var pickupState = usStates[random.Next(usStates.Length)];
                var deliveryState = usStates[random.Next(usStates.Length)];

                var pickupLatitude = pickupState.Latitude + ((decimal)(random.NextDouble() - 0.5) * 1m);
                var pickupLongitude = pickupState.Longitude + ((decimal)(random.NextDouble() - 0.5) * 1m);
                var deliveryLatitude = deliveryState.Latitude + ((decimal)(random.NextDouble() - 0.5) * 1m);
                var deliveryLongitude = deliveryState.Longitude + ((decimal)(random.NextDouble() - 0.5) * 1m);

                var jobId = $"JOB-{random.Next(10000, 99999)}";

                // Check if job exists
                using var checkJob = connection.CreateCommand();
                checkJob.Transaction = transaction;
                checkJob.CommandText = "SELECT COUNT(1) FROM Jobs WHERE Id = @Id";
                checkJob.Parameters.Add(CreateParam(checkJob, "@Id", jobId));
                var jobExists = Convert.ToInt32(checkJob.ExecuteScalar() ?? 0) > 0;
                if (jobExists)
                {
                    continue;
                }

                var totalAmount = Math.Round(job.BaseRate * 1.1m, 2); // Add 10% markup

                using var insertJob = connection.CreateCommand();
                insertJob.Transaction = transaction;
                insertJob.CommandText = @"
                INSERT INTO Jobs (Id, CustomerId, Title, PickupAddress, PickupLatitude, PickupLongitude,
                                DeliveryAddress, DeliveryLatitude, DeliveryLongitude,
                                Status, Priority, BaseRate, TotalAmount, ScheduledPickupTime, EstimatedDeliveryTime, AssignedDriverId, CreatedAt)
                VALUES (@Id, @CustomerId, @Title, @PickupAddress, @PickupLatitude, @PickupLongitude,
                       @DeliveryAddress, @DeliveryLatitude, @DeliveryLongitude,
                       @Status, @Priority, @BaseRate, @TotalAmount, @ScheduledPickupTime, @EstimatedDeliveryTime, @AssignedDriverId, @CreatedAt)";

                insertJob.Parameters.Add(CreateParam(insertJob, "@Id", jobId));
                insertJob.Parameters.Add(CreateParam(insertJob, "@CustomerId", customerId));
                insertJob.Parameters.Add(CreateParam(insertJob, "@Title", job.Title));
                insertJob.Parameters.Add(CreateParam(insertJob, "@PickupAddress", $"{random.Next(100, 9999)} {pickupState.Name} Ave"));
                insertJob.Parameters.Add(CreateParam(insertJob, "@PickupLatitude", pickupLatitude));
                insertJob.Parameters.Add(CreateParam(insertJob, "@PickupLongitude", pickupLongitude));
                insertJob.Parameters.Add(CreateParam(insertJob, "@DeliveryAddress", $"{random.Next(100, 9999)} {deliveryState.Name} St"));
                insertJob.Parameters.Add(CreateParam(insertJob, "@DeliveryLatitude", deliveryLatitude));
                insertJob.Parameters.Add(CreateParam(insertJob, "@DeliveryLongitude", deliveryLongitude));
                insertJob.Parameters.Add(CreateParam(insertJob, "@Status", (int)job.Status));
                insertJob.Parameters.Add(CreateParam(insertJob, "@Priority", (int)job.Priority));
                insertJob.Parameters.Add(CreateParam(insertJob, "@BaseRate", job.BaseRate));
                insertJob.Parameters.Add(CreateParam(insertJob, "@TotalAmount", totalAmount));
                insertJob.Parameters.Add(CreateParam(insertJob, "@ScheduledPickupTime", job.ScheduledPickupTime));
                insertJob.Parameters.Add(CreateParam(insertJob, "@EstimatedDeliveryTime", job.EstimatedDeliveryTime));
                insertJob.Parameters.Add(CreateParam(insertJob, "@AssignedDriverId", assignedDriverId));
                insertJob.Parameters.Add(CreateParam(insertJob, "@CreatedAt", DateTime.UtcNow));

                insertJob.ExecuteNonQuery();

                // Create job assignment if job is not pending
                if (job.Status != JobStatus.Pending)
                {
                    var jaId = $"JA-{jobId}";
                    using var insertJA = connection.CreateCommand();
                    insertJA.Transaction = transaction;
                    insertJA.CommandText = @"
                    INSERT INTO JobAssignments (Id, JobId, DriverId, AssignedAt, Status, CreatedAt)
                    VALUES (@Id, @JobId, @DriverId, @AssignedAt, @Status, @CreatedAt)";

                    insertJA.Parameters.Add(CreateParam(insertJA, "@Id", jaId));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@JobId", jobId));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@DriverId", assignedDriverId));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@AssignedAt", DateTime.UtcNow));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@Status", job.Status == JobStatus.Completed ? 2 : 1)); // Completed or Active
                    insertJA.Parameters.Add(CreateParam(insertJA, "@CreatedAt", DateTime.UtcNow));

                    insertJA.ExecuteNonQuery();
                }

                // Create invoice for completed jobs
                if (job.Status == JobStatus.Completed)
                {
                    var invoiceId = $"INV-{jobId}";
                    var invoiceNumber = $"INV-{jobId}";
                    var invoiceDate = job.EstimatedDeliveryTime.Date;
                    var dueDate = invoiceDate.AddDays(30);

                    using var insertInv = connection.CreateCommand();
                    insertInv.Transaction = transaction;
                    insertInv.CommandText = @"
                    INSERT INTO Invoices (Id, InvoiceNumber, CustomerId, JobId, InvoiceDate, DueDate, Status, SubTotal, TaxRate, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
                    VALUES (@Id, @InvoiceNumber, @CustomerId, @JobId, @InvoiceDate, @DueDate, @Status, @SubTotal, @TaxRate, @TaxAmount, @DiscountAmount, @TotalAmount, @CreatedAt)";

                    var taxRate = 0.08m; // 8% tax
                    var taxAmount = Math.Round(totalAmount * taxRate, 2);
                    var totalInvoice = Math.Round(totalAmount + taxAmount, 2);

                    insertInv.Parameters.Add(CreateParam(insertInv, "@Id", invoiceId));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@InvoiceNumber", invoiceNumber));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@CustomerId", customerId));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@JobId", jobId));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@InvoiceDate", invoiceDate));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@DueDate", dueDate));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@Status", 0)); // Pending
                    insertInv.Parameters.Add(CreateParam(insertInv, "@SubTotal", totalAmount));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@TaxRate", taxRate));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@TaxAmount", taxAmount));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@DiscountAmount", 0m));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@TotalAmount", totalInvoice));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@CreatedAt", DateTime.UtcNow));

                    insertInv.ExecuteNonQuery();

                    // Add invoice line item
                    using var insertLine = connection.CreateCommand();
                    insertLine.Transaction = transaction;
                    insertLine.CommandText = @"
                    INSERT INTO InvoiceLineItems (InvoiceId, Description, Quantity, UnitPrice, TaxApplicable, SortOrder)
                    VALUES (@InvoiceId, @Description, @Quantity, @UnitPrice, @TaxApplicable, @SortOrder)";

                    insertLine.Parameters.Add(CreateParam(insertLine, "@InvoiceId", invoiceId));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@Description", $"Delivery service for {job.CargoDetails.Description}"));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@Quantity", 1m));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@UnitPrice", totalAmount));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@TaxApplicable", true));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@SortOrder", 1));

                    insertLine.ExecuteNonQuery();
                }
            }
        });
    }

    public override void Down()
    {
        // Remove all seeded data
        Execute.WithConnection((connection, transaction) =>
        {
            using var delLines = connection.CreateCommand();
            delLines.Transaction = transaction;
            delLines.CommandText = "DELETE FROM InvoiceLineItems WHERE InvoiceId LIKE 'INV-JOB%'";
            delLines.ExecuteNonQuery();

            using var delInv = connection.CreateCommand();
            delInv.Transaction = transaction;
            delInv.CommandText = "DELETE FROM Invoices WHERE Id LIKE 'INV-JOB%'";
            delInv.ExecuteNonQuery();

            using var delJA = connection.CreateCommand();
            delJA.Transaction = transaction;
            delJA.CommandText = "DELETE FROM JobAssignments WHERE Id LIKE 'JA-JOB%'";
            delJA.ExecuteNonQuery();

            using var delJobs = connection.CreateCommand();
            delJobs.Transaction = transaction;
            delJobs.CommandText = "DELETE FROM Jobs WHERE Id LIKE 'JOB-%'";
            delJobs.ExecuteNonQuery();

            using var delDrivers = connection.CreateCommand();
            delDrivers.Transaction = transaction;
            delDrivers.CommandText = "DELETE FROM Drivers WHERE Email LIKE '%@drivers.logistics.com'";
            delDrivers.ExecuteNonQuery();

            using var delCustomers = connection.CreateCommand();
            delCustomers.Transaction = transaction;
            delCustomers.CommandText = "DELETE FROM Customers WHERE Id LIKE 'CUST-%'";
            delCustomers.ExecuteNonQuery();
        });
    }

    private static IDbDataParameter CreateParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        return p;
    }

    // Helper classes for data generation
    private class StateInfo
    {
        public StateInfo(string code, string name, decimal latitude, decimal longitude)
        {
            Code = code;
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
        }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }
    }

    private class DriverData
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string LicenseState { get; set; } = string.Empty;
        public DateTime LicenseExpiryDate { get; set; }
        public DateTime InsuranceExpiryDate { get; set; }
        public DriverStatus CurrentStatus { get; set; }
        public bool IsActive { get; set; }
        public VehicleInfo VehicleInfo { get; set; } = new();
    }

    private class VehicleInfo
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
    }

    private class JobData
    {
        public string Title { get; set; } = string.Empty;
        public JobStatus Status { get; set; }
        public JobPriority Priority { get; set; }
        public decimal BaseRate { get; set; }
        public DateTime ScheduledPickupTime { get; set; }
        public DateTime EstimatedDeliveryTime { get; set; }
        public CargoDetails CargoDetails { get; set; } = new();
    }

    private class CargoDetails
    {
        public string Description { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool IsHazardous { get; set; }
    }

    private class CustomerData
    {
        public string CompanyName { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public int PaymentTermsDays { get; set; }
        public CreditStatus CreditStatus { get; set; }
        public bool IsActive { get; set; }
    }
}
