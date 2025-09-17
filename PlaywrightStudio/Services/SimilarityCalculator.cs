using Microsoft.Extensions.AI;

namespace PlaywrightStudio.Services;

/// <summary>
/// Service for calculating similarity between embeddings
/// </summary>
public static class SimilarityCalculator
{
    /// <summary>
    /// Calculates cosine similarity between two embeddings
    /// </summary>
    /// <param name="embedding1">First embedding</param>
    /// <param name="embedding2">Second embedding</param>
    /// <returns>Cosine similarity score between -1 and 1</returns>
    public static float CalculateCosineSimilarity(Embedding<float> embedding1, Embedding<float> embedding2)
    {
        var span1 = embedding1.Vector.ToArray();
        var span2 = embedding2.Vector.ToArray();
        
        if (span1.Length != span2.Length)
        {
            throw new ArgumentException("Embeddings must have the same dimension");
        }

        float dotProduct = 0f;
        float magnitude1 = 0f;
        float magnitude2 = 0f;

        for (int i = 0; i < span1.Length; i++)
        {
            dotProduct += span1[i] * span2[i];
            magnitude1 += span1[i] * span1[i];
            magnitude2 += span2[i] * span2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0f || magnitude2 == 0f)
        {
            return 0f;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }

    /// <summary>
    /// Calculates cosine similarity between two float arrays
    /// </summary>
    /// <param name="vector1">First vector</param>
    /// <param name="vector2">Second vector</param>
    /// <returns>Cosine similarity score between -1 and 1</returns>
    public static float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same dimension");
        }

        float dotProduct = 0f;
        float magnitude1 = 0f;
        float magnitude2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0f || magnitude2 == 0f)
        {
            return 0f;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }

    /// <summary>
    /// Calculates cosine similarity between two ReadOnlySpan<float>
    /// </summary>
    /// <param name="span1">First span</param>
    /// <param name="span2">Second span</param>
    /// <returns>Cosine similarity score between -1 and 1</returns>
    public static float CalculateCosineSimilarity(ReadOnlySpan<float> span1, ReadOnlySpan<float> span2)
    {
        if (span1.Length != span2.Length)
        {
            throw new ArgumentException("Spans must have the same dimension");
        }

        float dotProduct = 0f;
        float magnitude1 = 0f;
        float magnitude2 = 0f;

        for (int i = 0; i < span1.Length; i++)
        {
            dotProduct += span1[i] * span2[i];
            magnitude1 += span1[i] * span1[i];
            magnitude2 += span2[i] * span2[i];
        }

        magnitude1 = MathF.Sqrt(magnitude1);
        magnitude2 = MathF.Sqrt(magnitude2);

        if (magnitude1 == 0f || magnitude2 == 0f)
        {
            return 0f;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }
}
