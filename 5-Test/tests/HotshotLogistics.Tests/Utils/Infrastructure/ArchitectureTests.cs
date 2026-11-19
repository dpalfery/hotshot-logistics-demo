using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
    ///     Architecture rules and layering tests.
    /// </summary>
    public class ArchitectureTests
    {
        private const string s_presentation = "HotshotLogistics.Api";
        private const string s_infrastructure = "HotshotLogistics.Infrastructure";
        private const string s_application = "HotshotLogistics.Application";
        private const string s_data = "HotshotLogistics.Data";

        [Fact]
        public void Domain_should_not_depend_on_other_layers()
        {
            TestResult result = Types.InAssembly(typeof(Driver).Assembly)
                .ShouldNot()
                .HaveDependencyOnAny(s_application, s_data, s_infrastructure, s_presentation)
                .GetResult();

            Assert.True(result.IsSuccessful, string.Join(',', result.FailingTypeNames ?? Array.Empty<string>()));
        }

        [Fact]
        public void Application_should_not_depend_on_presentation()
        {
            TestResult result = Types.InAssembly(typeof(DriverService).Assembly)
                .ShouldNot()
                .HaveDependencyOn(s_presentation)
                .GetResult();

            Assert.True(result.IsSuccessful, string.Join(',', result.FailingTypeNames ?? Array.Empty<string>()));
        }

        [Fact]
        public void Contracts_should_contain_interfaces_only()
        {
            IEnumerable<Type> nonInterfaces = Types.InAssembly(typeof(IJobRepository).Assembly)
                .That().AreNotInterfaces()
                .GetTypes();

            Assert.True(
                !nonInterfaces.Any(),
                "Shared DTOs belong in HotshotLogistics.Domain.DTOs: " + string.Join(',', nonInterfaces.Select(t => t.Name)));
        }

        [Fact]
        public void Repositories_should_be_internal()
        {
            TestResult result = Types.InAssembly(typeof(DriverRepository).Assembly)
                .That().HaveNameEndingWith("Repository")
                .Should().NotBePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, string.Join(',', result.FailingTypeNames ?? Array.Empty<string>()));
        }
    }
}
