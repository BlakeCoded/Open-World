public interface IPoolable
{
    bool IsReleased { get; }
    void OnSpawn();
    void OnDespawn();
}