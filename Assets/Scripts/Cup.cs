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
    public int ownerID;
    public bool isRevealed;

    public Cup()
    {
        cupType = CupType.EMPTY;
        isConsumed = false;
        ownerID = -1;
        isRevealed = false;
    }
}
