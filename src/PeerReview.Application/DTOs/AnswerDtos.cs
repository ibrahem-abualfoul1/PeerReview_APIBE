namespace PeerReview.Application.DTOs;
public record AnswerCreateDto(int QuestionId, int? QuestionItemId, string? Value);
public record AnswerUpdateDto(string? Value);
public record AssignRequest(List<int> QuestionIds, List<int> UserIds);
public class DashboardDto
{
    public Dictionary<string, object> Metrics { get; set; } = new();

    public List<SimpleItemDto> LatestAnswered { get; set; } = new();
    public List<SimpleFileDto> LatestFiles { get; set; } = new();

    // Admin Analytics
    public List<RankItemDto> TopUsers { get; set; } = new();
    public List<RankItemDto> TopReviewers { get; set; } = new();
    public List<RankItemDto> TopQuestions { get; set; } = new();
}

public class SimpleItemDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
}

public class SimpleFileDto
{
    public string FileName { get; set; } = "";
    public long Length { get; set; }
    public DateTime? Date { get; set; }
}

public class RankItemDto
{
    public string Name { get; set; } = "";
    public decimal Count { get; set; }
}
