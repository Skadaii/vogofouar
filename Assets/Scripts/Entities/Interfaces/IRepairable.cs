
public interface IRepairable
{
    bool NeedsRepairing();
    void Repair(float amount);
    void FullRepair();
}
