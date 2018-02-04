
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Noise
{
    public static List<Vector2> PoissonDiskSample(Vector2 min, Vector2 max, float diskR, int maxN)
    {
        const int maxNumTries = 100;

        float sqrDiskR = diskR * diskR;

        List<Vector2> samples = new List<Vector2>();
        for (int i = 0; i < maxN; i++)
        {
            Vector2 candidateSample = Vector2.zero;
            int numTries = 0;
            for (; numTries < maxNumTries; numTries++)
            {
                candidateSample = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
                if (samples.All(s => (s - candidateSample).sqrMagnitude >= sqrDiskR))
                    break;
            }

            if (numTries == maxNumTries)
                break;
            else
                samples.Add(candidateSample);
        }

        return samples;
    }
}

