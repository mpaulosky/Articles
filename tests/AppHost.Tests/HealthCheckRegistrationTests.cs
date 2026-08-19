// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     HealthCheckRegistrationTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost;

public class HealthCheckRegistrationTests
{
	[Fact]
	public async Task AddDefaultHealthChecksRegistersReadinessAndLivenessChecks()
	{
		// Arrange
		var builder = Host.CreateApplicationBuilder();

		// Act
		builder.AddServiceDefaults();
		await using var provider = builder.Services.BuildServiceProvider();
		var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
		var healthCheckService = provider.GetRequiredService<HealthCheckService>();

		// Fix: Use dedicated CancellationTokenSource with timeout to avoid CI/CD issues with TestContext.Current.CancellationToken
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var healthCheckResult = await healthCheckService.CheckHealthAsync(cts.Token);

		// Assert
		healthCheckOptions.Registrations.Should()
			.ContainSingle(registration => registration.Name == "self" && registration.Tags.Count == 0);
		healthCheckOptions.Registrations.Should().ContainSingle(registration =>
			registration.Name == "self-live" && registration.Tags.Contains("live"));
		healthCheckResult.Status.Should().Be(HealthStatus.Healthy);
	}

	[Fact]
	public async Task MapDefaultEndpointsRegistersHealthAndAlivenessRoutes()
	{
		// Arrange
		var builder =
			WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });

		// Fix: Add service defaults to register health check services before building the app
		builder.AddServiceDefaults();
		var app = builder.Build();

		// Act
		app.MapDefaultEndpoints();
		// Fix: GetRoutePatternsAsync now uses dedicated CancellationTokenSource to avoid TestContext timing issues
		var endpoints = await GetRoutePatternsAsync(app);

		// Assert
		endpoints.Should().Contain("/health");
		endpoints.Should().Contain("/alive");
	}

	[Fact]
	public async Task MapDefaultEndpointsDoesNotRegisterRoutesWhenDisabled()
	{
		// Arrange
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
		var app = builder.Build();

		// Act
		app.MapDefaultEndpoints();
		var endpoints = await GetRoutePatternsAsync(app);

		// Assert
		endpoints.Should().NotContain("/health");
		endpoints.Should().NotContain("/alive");
	}

	[Fact]
	public async Task MapDefaultEndpointsRegistersRoutesWhenEnabledByConfigurationOutsideDevelopment()
	{
		// Arrange
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["EnableHealthEndpoints"] = "true",
		});

		// Fix: Add service defaults to register health check services before building the app
		builder.AddServiceDefaults();
		var app = builder.Build();

		// Act
		app.MapDefaultEndpoints();
		// Fix: GetRoutePatternsAsync now uses dedicated CancellationTokenSource to avoid TestContext timing issues
		var endpoints = await GetRoutePatternsAsync(app);

		// Assert
		endpoints.Should().Contain("/health");
		endpoints.Should().Contain("/alive");
	}

	[Fact]
	public void ConfigureOpenTelemetryAddsOtlpExporterRegistrationsOnlyWhenConfigured()
	{
		// Arrange
		var builderWithoutEndpoint = Host.CreateApplicationBuilder();
		var builderWithEndpoint = Host.CreateApplicationBuilder();
		builderWithEndpoint.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
		});

		// Act
		builderWithoutEndpoint.ConfigureOpenTelemetry();
		builderWithEndpoint.ConfigureOpenTelemetry();

		var hasOtlpWithoutEndpoint = HasOtlpExporterOptionsRegistration(builderWithoutEndpoint.Services);
		var hasOtlpWithEndpoint = HasOtlpExporterOptionsRegistration(builderWithEndpoint.Services);

		// Assert
		hasOtlpWithoutEndpoint.Should().BeFalse();
		hasOtlpWithEndpoint.Should().BeTrue();
	}

	[Fact]
	public void ConfigureOpenTelemetryUsesAspireDashboardEndpointWhenStandardOtlpEndpointIsMissing()
	{
		// Arrange
		var builder = Host.CreateApplicationBuilder();
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "https://localhost:21244",
		});

		// Act
		builder.ConfigureOpenTelemetry();
		var hasOtlpWithDashboardEndpoint = HasOtlpExporterOptionsRegistration(builder.Services);

		// Assert
		hasOtlpWithDashboardEndpoint.Should().BeTrue();
	}

	[Fact]
	public void ConfigureOpenTelemetrySetsLoggingAndTracingOptions()
	{
		// Arrange
		var builder = Host.CreateApplicationBuilder();

		// Act
		builder.ConfigureOpenTelemetry();
		using var provider = builder.Services.BuildServiceProvider();

		var loggingOptions = provider.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;
		var tracingOptions = provider.GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value;

		// Assert
		loggingOptions.IncludeFormattedMessage.Should().BeTrue();
		loggingOptions.IncludeScopes.Should().BeTrue();
		tracingOptions.Filter.Should().NotBeNull();
		var healthContext = new DefaultHttpContext { Request = { Path = "/health" } };
		var aliveContext = new DefaultHttpContext { Request = { Path = "/alive" } };
		var articlesContext = new DefaultHttpContext { Request = { Path = "/articles" } };

		tracingOptions.Filter!(healthContext).Should().BeFalse();
		tracingOptions.Filter!(aliveContext).Should().BeFalse();
		tracingOptions.Filter!(articlesContext).Should().BeTrue();
	}

	private static bool HasOtlpExporterOptionsRegistration(IServiceCollection services)
	{
		return services.Any(descriptor =>
		{
			if (!descriptor.ServiceType.IsGenericType)
			{
				return false;
			}

			return descriptor.ServiceType.GetGenericArguments()
				.Any(argument => argument.Name.Equals("OtlpExporterOptions", StringComparison.Ordinal));
		});
	}

	private static async Task<string?[]> GetRoutePatternsAsync(WebApplication app)
	{
		// Fix: Use dedicated CancellationTokenSource with timeout instead of TestContext.Current.CancellationToken
		// This prevents race conditions and timing issues in CI/CD environments
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		await app.StartAsync(cts.Token).ConfigureAwait(false);
		try
		{
			return
			[
				.. app.Services.GetRequiredService<IEnumerable<EndpointDataSource>>()
					.SelectMany(dataSource => dataSource.Endpoints)
					.OfType<RouteEndpoint>()
					.Select(endpoint => endpoint.RoutePattern.RawText)
			];
		}
		finally
		{
			await app.StopAsync(cts.Token).ConfigureAwait(false);
		}
	}
}
