//----------------------
// GetOperationsOverviewQuery Tests (O5 - Operations Portal)
// Verifies aggregation of health-check data and O4 metric snapshots into the
// Operations read model, including unavailable-telemetry semantics.
//----------------------

using ATLAS.Application.Queries.Admin;
using ATLAS.Application.Telemetry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries.Admin
{
    public class GetOperationsOverviewQueryTests
    {
        private readonly Mock<HealthCheckService> _healthCheckMock;
        private readonly Mock<IOperationsMetricsSnapshot> _metricsMock = new();

        public GetOperationsOverviewQueryTests()
        {
            // HealthCheckService has no parameterless constructor; Moq can proxy it.
            _healthCheckMock = new Mock<HealthCheckService>();
        }

        private GetOperationsOverviewQueryHandler CreateHandler() =>
            new(_healthCheckMock.Object, _metricsMock.Object, NullLogger<GetOperationsOverviewQueryHandler>.Instance);

        private static HealthReport Report(params (string Name, HealthStatus Status, string? Description)[] entries)
        {
            var dict = entries.ToDictionary(
                e => e.Name,
                e => new HealthReportEntry(e.Status, e.Description, TimeSpan.FromMilliseconds(12), null, null));
            return new HealthReport(dict, TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public async Task Handle_HealthySystem_ReturnsOverallAndEntries()
        {
            _healthCheckMock
                .Setup(h => h.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Report(("database", HealthStatus.Healthy, "Database reachable")));
            _metricsMock.SetupGet(m => m.HasData).Returns(false);

            var result = await CreateHandler().Handle(new GetOperationsOverviewQuery(), CancellationToken.None);

            Assert.False(result.HealthUnavailable);
            Assert.Equal("Healthy", result.OverallHealth);
            Assert.Single(result.HealthEntries);
            Assert.Equal("database", result.HealthEntries[0].Name);
            Assert.Equal("Healthy", result.HealthEntries[0].Status);
        }

        [Fact]
        public async Task Handle_UnhealthyDependency_PreservesStatusAndDescription()
        {
            _healthCheckMock
                .Setup(h => h.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Report(
                    ("database", HealthStatus.Healthy, null),
                    ("key-vault", HealthStatus.Unhealthy, "Vault unreachable")));
            _metricsMock.SetupGet(m => m.HasData).Returns(false);

            var result = await CreateHandler().Handle(new GetOperationsOverviewQuery(), CancellationToken.None);

            Assert.Equal("Unhealthy", result.OverallHealth);
            var vault = Assert.Single(result.HealthEntries, e => e.Name == "key-vault");
            Assert.Equal("Unhealthy", vault.Status);
            Assert.Equal("Vault unreachable", vault.Description);
        }

        [Fact]
        public async Task Handle_HealthCheckThrows_HealthUnavailable_NotUnhealthy()
        {
            _healthCheckMock
                .Setup(h => h.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("health infrastructure failure"));
            _metricsMock.SetupGet(m => m.HasData).Returns(false);

            var result = await CreateHandler().Handle(new GetOperationsOverviewQuery(), CancellationToken.None);

            // Unavailable must be distinguishable from Unhealthy.
            Assert.True(result.HealthUnavailable);
            Assert.Null(result.OverallHealth);
            Assert.Empty(result.HealthEntries);
        }

        [Fact]
        public async Task Handle_NoMetricsRecorded_MetricsAreNull_NotZero()
        {
            _healthCheckMock
                .Setup(h => h.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Report(("database", HealthStatus.Healthy, null)));
            _metricsMock.SetupGet(m => m.HasData).Returns(false);

            var result = await CreateHandler().Handle(new GetOperationsOverviewQuery(), CancellationToken.None);

            // "No telemetry" must be distinguishable from zero.
            Assert.Null(result.ApplicationTransitions);
            Assert.Null(result.EmailSends);
        }

        [Fact]
        public async Task Handle_MetricsRecorded_ExposesCumulativeCounts()
        {
            _healthCheckMock
                .Setup(h => h.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Report(("database", HealthStatus.Healthy, null)));
            _metricsMock.SetupGet(m => m.HasData).Returns(true);
            _metricsMock.SetupGet(m => m.ApplicationTransitions).Returns(
                new Dictionary<string, long> { ["submitted"] = 5, ["approved"] = 2 });
            _metricsMock.SetupGet(m => m.EmailSends).Returns(
                new Dictionary<string, long> { ["success"] = 9, ["failure"] = 1 });

            var result = await CreateHandler().Handle(new GetOperationsOverviewQuery(), CancellationToken.None);

            Assert.NotNull(result.ApplicationTransitions);
            Assert.Equal(5, result.ApplicationTransitions!["submitted"]);
            Assert.Equal(2, result.ApplicationTransitions!["approved"]);
            Assert.Equal(9, result.EmailSends!["success"]);
            Assert.Equal(1, result.EmailSends!["failure"]);
        }

        [Fact]
        public async Task Handle_SetsCaptureTimestamp()
        {
            _healthCheckMock
                .Setup(h => h.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Report(("database", HealthStatus.Healthy, null)));
            _metricsMock.SetupGet(m => m.HasData).Returns(false);

            var before = DateTime.UtcNow.AddSeconds(-5);
            var result = await CreateHandler().Handle(new GetOperationsOverviewQuery(), CancellationToken.None);
            var after = DateTime.UtcNow.AddSeconds(5);

            Assert.InRange(result.CapturedAtUtc, before, after);
        }
    }
}
