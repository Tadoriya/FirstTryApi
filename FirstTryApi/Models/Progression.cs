namespace FirstTryApi.Models;


public class Progression
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Count { get; set; }
    public int TotalClickValue { get; set; }
    public int Multiplier { get; set; }
    public int BestScore { get; set; }


    public Progression() { }

    public Progression(int userid)
    {
        UserId = userid;
        Count = 0;
        TotalClickValue = 0;
        Multiplier = 1;
        BestScore = 0;
    }

    public void AddClick()
    {
        Count += TotalClickValue * Multiplier;
        if (Count > BestScore)
            BestScore = Count;
    }

    public int CalculateResetCost()
    {
        double factor = 1.5;
        double cost = 100 * (Math.Pow(factor, Multiplier - 1));
        return (int)Math.Floor(cost);
    }

    public int TryReset()
    {
        int cost = CalculateResetCost();
        if (Count < cost)
            return 0;
        Count = 0;
        Multiplier++;
        return 1;
    }
}