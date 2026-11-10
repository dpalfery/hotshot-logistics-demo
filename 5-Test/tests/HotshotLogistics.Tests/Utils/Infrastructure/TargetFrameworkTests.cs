using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
    ///     Guards that the tree targets .NET 10, through two mechanisms that prove different things
    ///     and are deliberately kept separate.
    ///     <para>
    ///         The six assembly facts read <see cref="TargetFrameworkAttribute" /> from a type in each
    ///         assembly this project references. That proves the assemblies that actually loaded are
    ///         .NET 10 builds, and it reaches only the six projects reached through a project
    ///         reference.
    ///     </para>
    ///     <para>
    ///         <see cref="AllProjectsInTree_WhenInspected_TargetNet10" /> reads the
    ///         <c>TargetFramework</c> property of every <c>.csproj</c> in the tree. That is what
    ///         covers the projects the assembly facts cannot see: MigrationRunner,
    ///         HotshotLogistics.DbSetup, HotshotLogistics.DbSetup.Tests,
    ///         HotshotLogistics.IntegrationTests and HotshotLogistics.Api.Tests.
    ///     </para>
    ///     <para>
    ///         Neither mechanism proves a project builds. An MSBuild run asserts no target framework,
    ///         so a clean Release build is not evidence for either fact, and neither fact is evidence
    ///         that the solution compiles. The two belong together.
    ///     </para>
    /// </summary>
    public class TargetFrameworkTests
    {
        /// <summary>
        ///     The framework name prefix that identifies a .NET 10 build. The major version is
        ///     asserted and no patch number is pinned, because the framework name carries the
        ///     version in the form <c>.NETCoreApp,Version=v10.0</c>.
        /// </summary>
        private const string s_net10FrameworkPrefix = ".NETCoreApp,Version=v10.";

        /// <summary>
        ///     The project-file target framework moniker for .NET 10. Only the major version is
        ///     asserted, so <c>net10.0</c> and platform-specific forms such as <c>net10.0-windows</c>
        ///     both satisfy it.
        /// </summary>
        private const string s_net10TargetFrameworkMoniker = "net10.";

        /// <summary>
        ///     The solution file used to locate the repository root from the test output directory.
        /// </summary>
        private const string s_solutionFileName = "HotshotLogistics.sln";

        [Fact]
        public void CoreAssembly_WhenInspected_TargetsNet10()
        {
            AssertTargetsNet10(typeof(JobStatus).Assembly);
        }

        [Fact]
        public void ApiAssembly_WhenInspected_TargetsNet10()
        {
            AssertTargetsNet10(typeof(Program).Assembly);
        }

        [Fact]
        public void ApplicationAssembly_WhenInspected_TargetsNet10()
        {
            AssertTargetsNet10(typeof(DriverService).Assembly);
        }

        [Fact]
        public void ContractsAssembly_WhenInspected_TargetsNet10()
        {
            AssertTargetsNet10(typeof(IDriverRepository).Assembly);
        }

        [Fact]
        public void DomainAssembly_WhenInspected_TargetsNet10()
        {
            AssertTargetsNet10(typeof(Driver).Assembly);
        }

        [Fact]
        public void DataAssembly_WhenInspected_TargetsNet10()
        {
            AssertTargetsNet10(typeof(DriverRepository).Assembly);
        }

        /// <summary>
        ///     Asserts the declared target framework of every project in the tree, which is the only
        ///     way to cover projects this test project does not reference.
        /// </summary>
        [Fact]
        public void AllProjectsInTree_WhenInspected_TargetNet10()
        {
            string repositoryRoot = FindRepositoryRoot();

            string[] projectFiles = Directory
                .EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(projectPath => !IsBuildOutputPath(projectPath))
                .OrderBy(projectPath => projectPath, StringComparer.Ordinal)
                .ToArray();

            projectFiles.Should().NotBeEmpty(
                $"every project under {repositoryRoot} must be discoverable from the located repository root");

            List<string> violations = [];
            foreach (string projectPath in projectFiles)
            {
                string? targetFramework = ReadTargetFramework(projectPath);
                if (targetFramework is null
                    || !targetFramework.StartsWith(s_net10TargetFrameworkMoniker, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(repositoryRoot, projectPath)}: {targetFramework ?? "<no TargetFramework property>"}");
                }
            }

            violations.Should().BeEmpty("every project in the tree must target .NET 10");
        }

        private static void AssertTargetsNet10(Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name ?? "<unnamed assembly>";
            TargetFrameworkAttribute? attribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
            string? frameworkName = attribute?.FrameworkName;

            frameworkName.Should().NotBeNull($"{assemblyName} must declare a TargetFrameworkAttribute");
            frameworkName!.Should().StartWith(
                s_net10FrameworkPrefix,
                $"{assemblyName} must be compiled for .NET 10");
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null
                   && !File.Exists(Path.Combine(directory.FullName, s_solutionFileName)))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull(
                $"the test must run from a checkout that contains {s_solutionFileName}");
            return directory!.FullName;
        }

        private static bool IsBuildOutputPath(string path)
        {
            return path.Contains(
                       $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                       StringComparison.Ordinal)
                   || path.Contains(
                       $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                       StringComparison.Ordinal);
        }

        private static string? ReadTargetFramework(string projectPath)
        {
            string projectContent = File.ReadAllText(projectPath);

            Match plural = Regex.Match(projectContent, @"<TargetFrameworks>([^<]+)</TargetFrameworks>");
            if (plural.Success)
            {
                return plural.Groups[1].Value.Trim();
            }

            Match singular = Regex.Match(projectContent, @"<TargetFramework>([^<]+)</TargetFramework>");
            return singular.Success ? singular.Groups[1].Value.Trim() : null;
        }
    }
}
