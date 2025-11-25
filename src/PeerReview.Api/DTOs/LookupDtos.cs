namespace PeerReview.Application.DTOs;
public record LookupCreateDto(string NameEn, string TypeEn, string Code);
public record LookupUpdateDto(string NameEn, string TypeEn, string Code);
public record SubLookupCreateDto(   string NameEn);
public record SubLookupUpdateDto(   string NameEn);
