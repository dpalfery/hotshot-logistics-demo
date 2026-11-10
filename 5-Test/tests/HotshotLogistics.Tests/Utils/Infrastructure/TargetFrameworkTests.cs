using System.Reflection;
using System.Runtime.Versioning;
using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
    ///     Guards that every HotshotLogistics assembly referenced by this test project is compiled
    ///     for .NET 10.
    ///     <para>
    ///         Only the six projects this test project references are covered here. MigrationRunner,
    ///         HotshotLogistics.DbSetup and the sibling test projects are not referenced by this
    ///         project, so this class does not overstate what it proves; those are covered by the
    ///         Release build gate instead.
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
    }
}
