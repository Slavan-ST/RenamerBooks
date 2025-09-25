// Services/OnnxNameNormalizer.cs (обновлённая версия)
using Microsoft.ML.Transforms.Onnx;
using RenameBooks.Interfaces;
using RenameBooks.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FuzzySharp;

namespace RenameBooks.Services
{
    public class OnnxNameNormalizer : INameNormalizer
    {
        private readonly OnnxEmbeddingService _embeddingService;
        private readonly KnownNamesStore _store;
        private readonly Dictionary<string, string> _authorCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _seriesCache = new(StringComparer.OrdinalIgnoreCase);

        // Пороги
        private const float EMBEDDING_THRESHOLD = 0.65f;
        private const int FUZZY_THRESHOLD = 70;

        public OnnxNameNormalizer(string modelPath, string assetsDir)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"ONNX модель не найдена: {modelPath}");

            _embeddingService = new OnnxEmbeddingService(modelPath);
            _store = new KnownNamesStore(assetsDir);
        }

        public string NormalizeAuthor(string rawAuthor)
        {
            if (string.IsNullOrWhiteSpace(rawAuthor))
                return "Неизвестный автор";

            if (_authorCache.TryGetValue(rawAuthor, out var cached))
                return cached;

            var knownAuthors = _store.LoadAuthors();
            var canonical = FindBestMatch(rawAuthor, knownAuthors, _embeddingService, EMBEDDING_THRESHOLD, FUZZY_THRESHOLD);

            // Если это действительно новое имя — добавляем в список
            if (canonical == rawAuthor && !knownAuthors.Contains(rawAuthor, StringComparer.OrdinalIgnoreCase))
            {
                _store.AddAuthor(rawAuthor);
                // Обновляем кеш, чтобы не добавлять дважды
                _authorCache[rawAuthor] = rawAuthor;
                return rawAuthor;
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
            var canonical = FindBestMatch(rawSeries, knownSeries, _embeddingService, EMBEDDING_THRESHOLD, FUZZY_THRESHOLD);

            if (canonical == rawSeries && !knownSeries.Contains(rawSeries, StringComparer.OrdinalIgnoreCase))
            {
                _store.AddSeries(rawSeries);
                _seriesCache[rawSeries] = rawSeries;
                return rawSeries;
            }

            _seriesCache[rawSeries] = canonical;
            return canonical;
        }

        private static string FindBestMatch(
            string input,
            List<string> knownValues,
            OnnxEmbeddingService embeddingService,
            float embeddingThreshold,
            int fuzzyThreshold)
        {
            if (knownValues.Count == 0)
                return input;

            // 1. Попытка через эмбеддинги
            try
            {
                var inputEmbedding = embeddingService.GetEmbedding(input);
                float bestScore = -1f;
                string bestMatch = input;

                foreach (var candidate in knownValues)
                {
                    try
                    {
                        var candEmbedding = embeddingService.GetEmbedding(candidate);
                        var sim = CosineSimilarity(inputEmbedding, candEmbedding);
                        if (sim > bestScore && sim > embeddingThreshold)
                        {
                            bestScore = sim;
                            bestMatch = candidate;
                        }
                    }
                    catch { /* skip bad candidate */ }
                }

                if (bestScore > embeddingThreshold)
                    return bestMatch;
            }
            catch { /* fallback to fuzzy */ }

            // 2. Fuzzy fallback
            var fuzzyBest = knownValues
                .Select(c => new { Candidate = c, Score = Fuzz.Ratio(input, c) })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (fuzzyBest != null && fuzzyBest.Score >= fuzzyThreshold)
                return fuzzyBest.Candidate;

            // 3. Ничего не подошло — возвращаем оригинал
            return input;
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length || a.Length == 0) return 0f;
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