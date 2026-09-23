using FluentAssertions;
using JournalApp.Application.Entries.Dtos;
using JournalApp.Application.Entries.Validators;
using JournalApp.Domain.Enums;

namespace JournalApp.Application.Tests.Entries;

public class EntryRequestValidatorTests
{
    private readonly CreateEntryRequestValidator _createValidator = new();
    private readonly UpdateEntryRequestValidator _updateValidator = new();

    [Fact]
    public void Validate_ValidCreateRequest_IsValid()
    {
        // Act
        var result = _createValidator.Validate(new CreateEntryRequest("Title", "Content", Mood.Good));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CreateRequestWithoutMood_IsValid()
    {
        // Act
        var result = _createValidator.Validate(new CreateEntryRequest("Title", "Content", null));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_CreateRequestEmptyTitle_HasTitleError(string title)
    {
        // Act
        var result = _createValidator.Validate(new CreateEntryRequest(title, "Content", null));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryRequest.Title));
    }

    [Fact]
    public void Validate_CreateRequestTitleTooLong_HasTitleError()
    {
        // Act
        var result = _createValidator.Validate(new CreateEntryRequest(new string('a', 101), "Content", null));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryRequest.Title));
    }

    [Fact]
    public void Validate_CreateRequestContentTooLong_HasContentError()
    {
        // Act
        var result = _createValidator.Validate(new CreateEntryRequest("Title", new string('a', 5001), null));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryRequest.Content));
    }

    [Fact]
    public void Validate_CreateRequestInvalidMood_HasMoodError()
    {
        // Act
        var result = _createValidator.Validate(new CreateEntryRequest("Title", "Content", (Mood)99));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEntryRequest.Mood));
    }

    [Fact]
    public void Validate_UpdateRequestEmptyContent_HasContentError()
    {
        // Act
        var result = _updateValidator.Validate(new UpdateEntryRequest("Title", "", null));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEntryRequest.Content));
    }

    [Fact]
    public void Validate_ValidUpdateRequest_IsValid()
    {
        // Act
        var result = _updateValidator.Validate(new UpdateEntryRequest("Title", "Content", Mood.Awful));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
