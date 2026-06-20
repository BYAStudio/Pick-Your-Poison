public enum PlayerState
{
    Healthy,
    Poisoned,
    Dead
}

public class Player
{
    public const int VarsayilanZehirlenmeSuresi = 3;
    public const int DoctorZehirlenmeSuresi = 4;

    public int playerID;
    public PlayerState currentState;
    public int poisonedTimer;
    public CharacterType characterType;
    public bool poisonedThisTurn;

    // Survivor: oyun boyunca 2 kez tur atlayabilir
    public int skipHakki;

    // Chemist: oyun boyunca 1 kez 1 zehir + 1 panzehir konumunu ogrenebilir
    public bool chemistAbilityUsed;

    // Detective: oyun boyunca 1 kez istedigi bardagin icine bakabilir
    public bool detectiveAbilityUsed;

    public bool IsAlive => currentState != PlayerState.Dead;

    public Player(int id)
    {
        playerID = id;
        currentState = PlayerState.Healthy;
        poisonedTimer = 0;
        characterType = CharacterType.None;
        skipHakki = 0;
        chemistAbilityUsed = false;
        detectiveAbilityUsed = false;
        poisonedThisTurn = false;
    }

    public int GetPoisonSurvivalTurns()
    {
        return characterType == CharacterType.Doctor ? DoctorZehirlenmeSuresi : VarsayilanZehirlenmeSuresi;
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
            poisonedThisTurn = true;
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
        poisonedThisTurn = false;
    }

    public void Die()
    {
        currentState = PlayerState.Dead;
        poisonedTimer = 0;
        poisonedThisTurn = false;
    }
}
