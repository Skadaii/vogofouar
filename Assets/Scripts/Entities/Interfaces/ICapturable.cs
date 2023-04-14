public interface ICapturable
{
    void Capture(int amount, ETeam team);
    void FinalizeCapture(ETeam team);
    public ETeam CapturingTeam { get; protected set; }
}
