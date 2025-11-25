using PeerReview.Domain.Enums;

namespace PeerReview.Application.DTOs;

// عنصر داخل السؤال (Item)
public record QuestionItemDto(
    int Id,
        string TextEn,

    QuestionType Type,
    bool IsRequired,
        string? OptionsCsvEn

);

// عند الإنشاء
public record QuestionCreateDto(
    string TitleEn,
    string? DescriptionEn,
    int CategoryId,
    int? SubCategoryId,
    List<int> Items
);

public record QuestionItemCreateDto(
        string TextEn,
        QuestionType Type,
    bool IsRequired,
        string? OptionsCsvEn
);

// عند التحديث
public record QuestionItemUpdateDto(
    int? Id,                 // null = عنصر جديد
        string TextEn,
        QuestionType Type,
    bool IsRequired,
        string? OptionsCsvEn
);
public record QuestionUpdateDto(
    string TitleEn,
    string? DescriptionEn,
    int CategoryId,
    int? SubCategoryId,
    List<int> Items
);

// عند العرض
public record QuestionDto(
    int Id,
    string TitleEn,
    string? DescriptionEn,
    int CategoryId,
    int? SubCategoryId,
    string? CategoryName,
    string? SubCategoryName,
    List<QuestionItemDto> Items
);
