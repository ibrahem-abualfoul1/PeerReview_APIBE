namespace PeerReview.Application.DTOs;
public record LoginRequest(string UserName, string Password);
public record LoginResponse(string Token, string UserName, string Role);
public record RegisterRequest(string UserName, string FullName, string Email, string Password, int? RoleId);
