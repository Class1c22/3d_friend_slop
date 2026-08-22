using UnityEngine;

public static class FalloffGenerator
{
    // Повертає масив [0..1], де 0 = центр острова (без спаду), 1 = дуже далеко від центру (буде водою)
    public static float[,] GenerateFalloffMap(int size, float a = 3f, float b = 2.2f)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value, a, b);
            }
        }
        return map;
    }

    private static float Evaluate(float value, float a, float b)
    {
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}