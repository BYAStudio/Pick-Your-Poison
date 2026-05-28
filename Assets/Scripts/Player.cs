public enum PlayerState
{
    Healthy,
    Poisoned,
    Dead
}

public class Player
{
    public const int VarsayilanZehirlenmeSuresi = 3;

    public int playerID;
    public PlayerState currentState;
    public int poisonedTimer;

    public bool IsAlive => currentState != PlayerState.Dead;

    public Player(int id)
    {
        playerID = id;
        currentState = PlayerState.Healthy;
        poisonedTimer = 0;
    }

    /// <summary>
    /// Healthy → Poisoned (3 tur). Poisoned iken tekrar zehir → anında ölüm (GDD §3).
    /// </summary>
    public void ApplyPoison(int turnsToSurvive = VarsayilanZehirlenmeSuresi)
    {
        if (currentState == PlayerState.Dead)
            return;

        if (currentState == PlayerState.Healthy)
        {
            currentState = PlayerState.Poisoned;
            poisonedTimer = turnsToSurvive;
            return;
        }

        if (currentState == PlayerState.Poisoned)
            Die();
    }

    /// <summary>
    /// Tur sonunda çağrılır; süre 0'a inerse ölür.
    /// </summary>
    public void TickPoisonedTimer()
    {
        if (currentState != PlayerState.Poisoned)
            return;

        poisonedTimer--;

        if (poisonedTimer <= 0)
            Die();
    }

    public void CurePoison()
    {
        if (currentState != PlayerState.Poisoned)
            return;

        currentState = PlayerState.Healthy;
        poisonedTimer = 0;
    }

    public void Die()
    {
        currentState = PlayerState.Dead;
        poisonedTimer = 0;
    }
}
