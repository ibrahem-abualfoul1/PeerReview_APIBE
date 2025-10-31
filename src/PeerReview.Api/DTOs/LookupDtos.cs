namespace PeerReview.Application.DTOs;
public record LookupCreateDto(string Name, string Type);
public record LookupUpdateDto(string Name, string Type);
public record SubLookupCreateDto(int LookupId, string Name);
public record SubLookupUpdateDto(int LookupId ,string Name  );
