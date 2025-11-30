using FirstTryApi.Models;

public record ErrorResponse(string MessageErr, string Code);
public record ClickResponse(int Count, int Multiplier);
public record ResetCostResponse(int Cost);
public record BestScoreResponse(int Userid, int BestScore);
public class GlobaleScore
{
    public static int UserId { get; set; } = 0;
    public static int BestScore { get; set; } = 0;
}
