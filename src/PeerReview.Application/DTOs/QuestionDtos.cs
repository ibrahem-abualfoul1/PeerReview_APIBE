using PeerReview.Domain.Enums;

namespace PeerReview.Application.DTOs;

// عنصر داخل السؤال (Item)
public record QuestionItemDto(
    int Id,
    string TextAr,
        string TextEn,

    QuestionType Type,
    bool IsRequired,
    string? OptionsCsvAr,
        string? OptionsCsvEn

);

// عند الإنشاء
public record QuestionCreateDto(
    string TitleAr,
    string? DescriptionAr,
    string TitleEn,
    string? DescriptionEn,
    int CategoryId,
    int? SubCategoryId,
    List<QuestionItemCreateDto> Items
);

public record QuestionItemCreateDto(
string TextAr,
        string TextEn,
        QuestionType Type,
    bool IsRequired,
    string? OptionsCsvAr,
        string? OptionsCsvEn
);

// عند التحديث
public record QuestionItemUpdateDto(
    int? Id,                 // null = عنصر جديد
string TextAr,
        string TextEn,
        QuestionType Type,
    bool IsRequired,
    string? OptionsCsvAr,
        string? OptionsCsvEn
);
public record QuestionUpdateDto(
    string TitleAr,
    string? DescriptionAr,
    string TitleEn,
    string? DescriptionEn,
    int CategoryId,
    int? SubCategoryId,
    List<QuestionItemUpdateDto> Items
);

// عند العرض
public record QuestionDto(
    int Id,
    string TitleAr,
    string? DescriptionAr,
    string TitleEn,
    string? DescriptionEn,
    int CategoryId,
    int? SubCategoryId,
    string? CategoryName,
    string? SubCategoryName,
    List<QuestionItemDto> Items
);
