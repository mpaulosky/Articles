// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostConfigurationTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost;

public class AppHostConfigurationTests
{
	private const string TestingEnvironment = "Testing";

	[Fact]
	public void GetAspNetCoreEnvironmentReturnsConfiguredValue()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["ASPNETCORE_ENVIRONMENT"] = TestingEnvironment, })
			.Build();

		// Act
		var result = AppHostConfiguration.GetAspNetCoreEnvironment(configuration);

		// Assert
		result.Should().Be(TestingEnvironment);
	}

	[Fact]
	public void GetAspNetCoreEnvironmentUsesEnvironmentVariableWhenConfigurationIsMissing()
	{
		// Arrange
		Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", TestingEnvironment);
		var configuration = new ConfigurationBuilder().Build();

		try
		{
			// Act
			var result = AppHostConfiguration.GetAspNetCoreEnvironment(configuration);

			// Assert
			result.Should().Be(TestingEnvironment);
		}
		finally
		{
			Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
		}
	}

	[Fact]
	public void GetAspNetCoreEnvironmentReturnsDefaultWhenNoValueIsProvided()
	{
		// Arrange
		Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
		var configuration = new ConfigurationBuilder().Build();

		// Act
		var result = AppHostConfiguration.GetAspNetCoreEnvironment(configuration);

		// Assert
		result.Should().Be(Environments.Development);
	}

	[Theory]
	[InlineData("1", true)]
	[InlineData("yes", true)]
	[InlineData("on", true)]
	[InlineData("0", false)]
	[InlineData("not-a-boolean", false)]
	public void IsDisabledParsesCommonTruthyAndNonTruthyValues(string configuredValue, bool expected)
	{
		// Arrange
		const string settingName = "DisableRabbitMq";
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { [settingName] = configuredValue, })
			.Build();

		// Act
		var result = AppHostConfiguration.IsDisabled(configuration, settingName);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void IsDisabledUsesEnvironmentVariableWhenConfigurationIsMissing()
	{
		// Arrange
		const string settingName = "DisableRedis";
		Environment.SetEnvironmentVariable(settingName, "true");
		var configuration = new ConfigurationBuilder().Build();

		try
		{
			// Act
			var result = AppHostConfiguration.IsDisabled(configuration, settingName);

			// Assert
			result.Should().BeTrue();
		}
		finally
		{
			Environment.SetEnvironmentVariable(settingName, null);
		}
	}

	[Fact]
	public void IsDisabledUsesEnvironmentValueWhenConfigurationValueIsWhitespace()
	{
		// Arrange
		const string settingName = "DisableApi";
		Environment.SetEnvironmentVariable(settingName, "true");
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { [settingName] = "   ", })
			.Build();

		try
		{
			// Act
			var result = AppHostConfiguration.IsDisabled(configuration, settingName);

			// Assert
			result.Should().BeTrue();
		}
		finally
		{
			Environment.SetEnvironmentVariable(settingName, null);
		}
	}

	[Fact]
	public void IsDisabledReturnsFalseWhenConfigurationAndEnvironmentAreMissing()
	{
		// Arrange
		const string settingName = "DisableSmtp";
		Environment.SetEnvironmentVariable(settingName, null);
		var configuration = new ConfigurationBuilder().Build();

		// Act
		var result = AppHostConfiguration.IsDisabled(configuration, settingName);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void IsDisabledThrowsWhenSettingNameIsMissing()
	{
		// Arrange
		var configuration = new ConfigurationBuilder().Build();

		// Act
		Action act = () => AppHostConfiguration.IsDisabled(configuration, " ");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void IsDisabledPrefersConfigurationValueOverEnvironmentVariable()
	{
		// Arrange
		const string settingName = "DisableMongoDb";
		Environment.SetEnvironmentVariable(settingName, "true");
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { [settingName] = "false", })
			.Build();

		try
		{
			// Act
			var result = AppHostConfiguration.IsDisabled(configuration, settingName);

			// Assert
			result.Should().BeFalse();
		}
		finally
		{
			Environment.SetEnvironmentVariable(settingName, null);
		}
	}
}