using PeerReview.Domain.Enums;
namespace PeerReview.Application.DTOs;
public record QuestionItemDto(int Id, string Text, QuestionType Type, bool IsRequired, string? OptionsCsv, int? ParentItemId, string? ShowWhenValue);
public record QuestionCreateDto(string Title, string? Description, int? CategoryId, List<QuestionItemCreateDto> Items);
public record QuestionItemCreateDto(string Text, QuestionType Type, bool IsRequired, string? OptionsCsv, int? ParentItemId, string? ShowWhenValue);
public record QuestionUpdateDto(string Title, string? Description, int? CategoryId, List<QuestionItemCreateDto> Items);
public record QuestionDto(int Id, string Title, string? Description, int? CategoryId, List<QuestionItemDto> Items);
