// Services/PythonEmbeddingService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace RenameBooks.Services
{
    public class PythonEmbeddingService
    {
        private readonly string _scriptPath;

        public PythonEmbeddingService(string scriptPath)
        {
            _scriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));
            if (!File.Exists(_scriptPath))
                throw new FileNotFoundException($"Скрипт не найден: {_scriptPath}");
        }

        /// <summary>
        /// Получает эмбеддинг для текста через Python-скрипт.
        /// </summary>
        /// <returns>Массив float длиной 384</returns>
        public float[] GetEmbedding(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Текст не может быть пустым", nameof(text));

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{_scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);

            // Пишем напрямую в StandardInput (он уже StreamWriter)
            process.StandardInput.Write(text);
            process.StandardInput.Close(); // ← обязательно закрыть, чтобы Python завершил чтение

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Ошибка Python-скрипта (код {process.ExitCode}): {error}");
            }

            try
            {
                var embedding = JsonSerializer.Deserialize<float[]>(output);
                if (embedding?.Length != 384)
                    throw new InvalidOperationException("Неверный размер эмбеддинга");
                return embedding;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Не удалось распарсить JSON: {output}", ex);
            }
        }
    }
}