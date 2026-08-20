using UnityEngine;
using WorldGen.Terrain;
using Unity.Mathematics;

public static class Noise
{
    public static float Sample(float x, float z)
    {
        int x0 = (int)math.floor(x);
        int x1 = x0 + 1;

        int z0 = (int)math.floor(z);
        int z1 = z0 + 1;

        float a = Hash(x0, z0);
        float b = Hash(x1, z0);
        float c = Hash(x0, z1);
        float d = Hash(x1, z1);

        float tx = x - x0;
        float tz = z - z0;

        tx = Fade(tx);
        tz = Fade(tz);

        float ab = Lerp(a, b, tx);
        float cd = Lerp(c, d, tx);

        float height = Lerp(ab, cd, tz);

        return height;
    }

    const int PRIME_X = 73856093;
    const int PRIME_Z = 19349663;

    private static float Hash(int x, int z)
    {
        unchecked
        {
            int n = x * PRIME_X + z * PRIME_Z;

            n ^= n << 13;

            n = n * (n * n * 15731 + 789221) + 1376312589;

            n &= 0x7fffffff;

            return n / (float)int.MaxValue;
        }
    }

    private static float Fade(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public static float FractalNoise(float x, float z, int octaves, float frequency, float persistence)
    {
        float total = 0f;
        float amplitude = 1f;
        float amplitudeSum = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float value = Sample(x * frequency, z * frequency);

            value = value * 2f - 1f;

            total += value * amplitude;
            amplitudeSum += amplitude;

            frequency *= 2f;
            amplitude *= persistence;
        }

        return total / amplitudeSum;
    }

    public static float SampleHeight(float worldX, float worldZ, NoiseSettings noise)
    {
        float noiseX = worldX * noise.Scale + noise.OffsetX;
        float noiseZ = worldZ * noise.Scale + noise.OffsetZ;

        float height = FractalNoise(noiseX, noiseZ, noise.Octaves, noise.Lacunarity, noise.Persistence) * noise.HeightMultiplier;

        return height;
    }
}
