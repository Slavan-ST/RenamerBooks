using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Utils
{
    public static class VectorUtils
    {
        /// <summary>
        /// Вычисляет косинусное сходство между двумя векторами.
        /// </summary>
        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException("Векторы разной длины");

            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            return dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        /// <summary>
        /// Простая кластеризация "жадным" методом.
        /// Группирует строки с сходством >= threshold.
        /// </summary>
        public static Dictionary<string, string> ClusterAndNormalize(
            string[] texts,
            float[][] embeddings,
            float similarityThreshold = 0.7f)
        {
            var normalized = new Dictionary<string, string>();
            var used = new bool[texts.Length];

            for (int i = 0; i < texts.Length; i++)
            {
                if (used[i]) continue;

                var cluster = new List<(string text, int index)> { (texts[i], i) };
                used[i] = true;

                // Сравниваем со всеми последующими
                for (int j = i + 1; j < texts.Length; j++)
                {
                    if (used[j]) continue;
                    float sim = CosineSimilarity(embeddings[i], embeddings[j]);
                    if (sim >= similarityThreshold)
                    {
                        cluster.Add((texts[j], j));
                        used[j] = true;
                    }
                }

                // Выбираем каноническое имя — самое длинное (часто полное)
                var canonical = cluster.OrderByDescending(x => x.text.Length).First().text;
                foreach (var (text, _) in cluster)
                {
                    normalized[text] = canonical;
                }
            }

            return normalized;
        }
    }
}
