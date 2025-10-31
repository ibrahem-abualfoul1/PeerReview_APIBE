namespace PeerReview.Application.DTOs;
public record AnswerCreateDto(int QuestionId, int? QuestionItemId, string? Value);
public record AnswerUpdateDto(string? Value);
public record AssignRequest(List<int> QuestionIds, List<int> UserIds);
public class DashboardDto { public Dictionary<string, object> Metrics { get; set; } = new(); }
