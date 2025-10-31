namespace PeerReview.Application.DTOs;
public record UserCreateDto(string UserName, string FullName, string Email, string Password, int RoleId);
public record UserUpdateDto(string FullName, string Email, bool IsActive, int RoleId);
