using System.Diagnostics;
using System.Reflection;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.DbSetup
{
    public class MigrationInvoker
    {
        private readonly ILogger<MigrationInvoker> _logger;

        public MigrationInvoker(ILogger<MigrationInvoker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> RunMigrationsAsync(string connectionString, string databaseName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting migration run for database: {DatabaseName}", databaseName);

            try
            {
                // First, try in-process migration if the assembly is available
                Assembly migrationAssembly;
                try
                {
                    migrationAssembly = Assembly.Load("HotshotLogistics.Data");
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning(
                        "HotshotLogistics.Data assembly not found, falling back to subprocess invocation");
                    return await RunMigrationsAsSubprocessAsync(connectionString, cancellationToken);
                }

                _logger.LogInformation("Found HotshotLogistics.Data assembly, running migrations in-process");
                return RunMigrationsInProcess(connectionString, migrationAssembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run migrations for database {DatabaseName}", databaseName);
                throw;
            }
        }

        private bool RunMigrationsInProcess(string connectionString, Assembly migrationAssembly)
        {
            try
            {
                _logger.LogInformation("Running migrations in-process using FluentMigrator");

                ServiceProvider serviceProvider = new ServiceCollection()
                    .AddFluentMigratorCore()
                    .ConfigureRunner(rb => rb
                        .AddSqlServer()
                        .WithGlobalConnectionString(connectionString)
                        .ScanIn(migrationAssembly).For.Migrations())
                    .AddLogging(lb => lb.AddFluentMigratorConsole())
                    .BuildServiceProvider(false);

                using IServiceScope scope = serviceProvider.CreateScope();
                IMigrationRunner runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

                // Check if there are any migrations to run
                SortedList<long, IMigrationInfo> migrationInfos = runner.MigrationLoader.LoadMigrations();
                if (!migrationInfos.Any())
                {
                    _logger.LogInformation("No migrations found to run");
                    return true;
                }

                _logger.LogInformation("Found {MigrationCount} migrations to run", migrationInfos.Count);

                // Run the migrations
                runner.MigrateUp();

                _logger.LogInformation("Successfully completed all migrations in-process");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run migrations in-process");
                throw;
            }
        }

        private async Task<bool> RunMigrationsAsSubprocessAsync(string connectionString,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Running migrations as subprocess");

                // Find the MigrationRunner project
                string currentDirectory = Directory.GetCurrentDirectory();
                string? solutionDirectory = FindSolutionDirectory(currentDirectory);

                if (solutionDirectory == null)
                {
                    throw new InvalidOperationException("Could not find solution directory");
                }

                string migrationRunnerPath = Path.Combine(
                    solutionDirectory,
                    "4-Persistence",
                    "MigrationRunner",
                    "bin",
                    "Debug",
                    "net10.0",
                    "MigrationRunner.dll"
                );

                if (!File.Exists(migrationRunnerPath))
                {
                    // Try to build the MigrationRunner first
                    _logger.LogInformation("MigrationRunner.dll not found, attempting to build it");
                    await BuildMigrationRunnerAsync(solutionDirectory, cancellationToken);

                    if (!File.Exists(migrationRunnerPath))
                    {
                        throw new FileNotFoundException(
                            $"MigrationRunner.dll not found at {migrationRunnerPath} after build attempt");
                    }
                }

                // Run the migration runner as a subprocess
                ProcessStartInfo startInfo = new()
                {
                    FileName = "dotnet",
                    Arguments = $"\"{migrationRunnerPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    EnvironmentVariables =
                    {
                        ["DB_CONNECTION_STRING"] = connectionString
                    }
                };

                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start MigrationRunner process");
                }

                string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                string error = await process.StandardError.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("MigrationRunner subprocess failed with exit code {ExitCode}. Error: {Error}",
                        process.ExitCode, error);
                    throw new InvalidOperationException($"MigrationRunner failed with exit code {process.ExitCode}");
                }

                if (!string.IsNullOrEmpty(output))
                {
                    _logger.LogInformation("MigrationRunner output: {Output}", output);
                }

                _logger.LogInformation("Successfully completed migrations via subprocess");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run migrations as subprocess");
                throw;
            }
        }

        private async Task BuildMigrationRunnerAsync(string solutionDirectory,
            CancellationToken cancellationToken = default)
        {
            string migrationRunnerProjectPath = Path.Combine(solutionDirectory, "4-Persistence", "MigrationRunner");

            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = migrationRunnerProjectPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start build process for MigrationRunner");
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to build MigrationRunner with exit code {process.ExitCode}");
            }

            _logger.LogInformation("Successfully built MigrationRunner project");
        }

        private static string? FindSolutionDirectory(string startDirectory)
        {
            DirectoryInfo? currentDirectory = new(startDirectory);

            while (currentDirectory != null)
            {
                FileInfo[] solutionFiles = currentDirectory.GetFiles("*.sln");
                if (solutionFiles.Length > 0)
                {
                    return currentDirectory.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }

            return null;
        }
    }
}
