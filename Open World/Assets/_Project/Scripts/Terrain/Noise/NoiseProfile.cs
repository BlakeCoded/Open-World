[System.Serializable]
public class NoiseProfile
{
    public int Octaves = 3; // How many times each thing is applied
    public float Scale = 0.01f; // How large features are High scale = small features, Low Scale = large features
    public float Persistence = 0.5f; // How Strong Smaller Details are
    public float Lacunarity = 2f; // How Much Smaller Each Detail gets
    public float HeightMultiplier = 50f; // total height multiplier min height -50f, max 50f.
}

public struct NoiseSettings
{
    public float Scale;
    public int Octaves;
    public float Persistence;
    public float Lacunarity;
    public float HeightMultiplier;
    public float OffsetX;
    public float OffsetZ;
}