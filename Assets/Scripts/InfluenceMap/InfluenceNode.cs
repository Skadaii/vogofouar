public class InfluenceNode : Node
{
    public ETeam team;
    public float value = 0f;

    public bool SetValue(ETeam f, float v)
    {
        if (f == ETeam.Neutral)
        {
            team = f; value = v;
            return true;
        }

        if (f == team)
        {
            value += v;
            return true;
        }
        else if (v > value)
        {
            value = v;
            team = f;
            return true;
        }
        return false;
    }
}
