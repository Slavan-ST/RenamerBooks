// Services/PythonNameNormalizer.cs
using RenameBooks.Interfaces;
using RenameBooks.Models; // ← ваш KnownNamesStore
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RenameBooks.Services
{
    public class PythonNameNormalizer : INameNormalizer
    {
        private readonly PythonEmbeddingService _embeddingService;
        private readonly KnownNamesStore _store;
        private readonly Dictionary<string, string> _authorCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _seriesCache = new(StringComparer.OrdinalIgnoreCase);

        public PythonNameNormalizer(string scriptPath, string assetsDir)
        {
            _embeddingService = new PythonEmbeddingService(scriptPath);
            _store = new KnownNamesStore(assetsDir);
        }

        public string NormalizeAuthor(string rawAuthor)
        {
            if (string.IsNullOrWhiteSpace(rawAuthor))
                return "Неизвестный автор";

            if (_authorCache.TryGetValue(rawAuthor, out var cached))
                return cached;

            var knownAuthors = _store.LoadAuthors();
            var canonical = FindBestMatch(rawAuthor, knownAuthors);

            if (canonical == rawAuthor && !knownAuthors.Contains(rawAuthor, StringComparer.OrdinalIgnoreCase))
            {
                _store.AddAuthor(rawAuthor);
            }

            _authorCache[rawAuthor] = canonical;
            return canonical;
        }

        public string NormalizeSeries(string rawSeries)
        {
            if (string.IsNullOrWhiteSpace(rawSeries))
                return string.Empty;

            if (_seriesCache.TryGetValue(rawSeries, out var cached))
                return cached;

            var knownSeries = _store.LoadSeries();
            var canonical = FindBestMatch(rawSeries, knownSeries);

            if (canonical == rawSeries && !knownSeries.Contains(rawSeries, StringComparer.OrdinalIgnoreCase))
            {
                _store.AddSeries(rawSeries);
            }

            _seriesCache[rawSeries] = canonical;
            return canonical;
        }

        private string FindBestMatch(string input, List<string> knownValues)
        {
            if (knownValues.Count == 0)
                return input;

            try
            {
                var inputEmbedding = _embeddingService.GetEmbedding(input);
                float bestScore = -1f;
                string bestMatch = input;

                foreach (var candidate in knownValues)
                {
                    try
                    {
                        var candEmbedding = _embeddingService.GetEmbedding(candidate);
                        var sim = CosineSimilarity(inputEmbedding, candEmbedding);
                        if (sim > bestScore && sim > 0.65f)
                        {
                            bestScore = sim;
                            bestMatch = candidate;
                        }
                    }
                    catch { /* skip */ }
                }

                if (bestScore > 0.65f)
                    return bestMatch;
            }
            catch { /* fallback to fuzzy */ }

            // Fallback на FuzzySharp (если установлен)
            return FallbackToFuzzy(input, knownValues);
        }

        private static string FallbackToFuzzy(string input, List<string> knownValues)
        {
#if FUZZY_SHARP
            var best = knownValues
                .Select(c => new { Candidate = c, Score = FuzzySharp.Fuzz.Ratio(input, c) })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            return best?.Score > 70 ? best.Candidate : input;
#else
            return input;
#endif
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0f;
            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            return (normA == 0 || normB == 0) ? 0 : dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}