
public interface IRepairable
{
    bool NeedsRepairing();
    float Repair(float amount);
    void FullRepair();
}
