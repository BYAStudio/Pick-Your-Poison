public enum CupType
{
    EMPTY,
    POISON,
    ANTIDOTE
}

public class Cup
{
    public CupType cupType;
    public bool isConsumed;
    public int ownerID;       // Bardagi zehirleyen/olusturan kisinin ID'si
    public int consumedByID; // Bardagi icen kisinin ID'si (-1 = icilmemis)
    public bool isRevealed;

    public Cup()
    {
        cupType = CupType.EMPTY;
        isConsumed = false;
        ownerID = -1;
        consumedByID = -1;
        isRevealed = false;
    }
}
