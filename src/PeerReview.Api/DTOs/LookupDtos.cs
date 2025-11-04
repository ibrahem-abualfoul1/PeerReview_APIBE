namespace PeerReview.Application.DTOs;
public record LookupCreateDto(string NameEn, string TypeEn, string NameAr, string TypeAr, string Code);
public record LookupUpdateDto(string NameEn, string TypeEn, string NameAr, string TypeAr, string Code);
public record SubLookupCreateDto( string Code, string NameAr, string NameEn);
public record SubLookupUpdateDto( string Code, string NameAr, string NameEn);
