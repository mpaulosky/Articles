using FluentAssertions;

using Web.Components.Features.Categories.Commands;
using Web.Components.Features.Categories.Validators;

namespace Web.Tests.Features.Categories;

public class CategoryValidatorsTests
{
	private readonly CreateCategoryCommandValidator _createValidator = new();
	private readonly UpdateCategoryCommandValidator _updateValidator = new();

	[Fact]
	public void CreateCategoryCommandValidator_RejectsInvalidNameAndDescription()
	{
		// Arrange
		var command = new CreateCategoryCommand("A", "bad");

		// Act
		var result = _createValidator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCategoryCommand.Name));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCategoryCommand.Description));
	}

	[Fact]
	public void CreateCategoryCommandValidator_AllowsValidCategory()
	{
		// Arrange
		var command = new CreateCategoryCommand("Technology", "This description is long enough.");

		// Act
		var result = _createValidator.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
		result.Errors.Should().BeEmpty();
	}

	[Fact]
	public void UpdateCategoryCommandValidator_RejectsEmptyIdAndShortValues()
	{
		// Arrange
		var command = new UpdateCategoryCommand(string.Empty, "A", "bad");

		// Act
		var result = _updateValidator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCategoryCommand.Id));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCategoryCommand.Name));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCategoryCommand.Description));
	}
}
