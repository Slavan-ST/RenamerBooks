using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace RenameBooks.Models
{
    public class KnownNamesStore
    {
        private readonly string _authorsPath;
        private readonly string _seriesPath;
        private readonly object _lock = new();

        public KnownNamesStore(string assetsDir)
        {
            _authorsPath = Path.Combine(assetsDir, "authors.json");
            _seriesPath = Path.Combine(assetsDir, "series.json");

            EnsureFileExists(_authorsPath, "[]");
            EnsureFileExists(_seriesPath, "[]");
        }

        public List<string> LoadAuthors() => LoadList(_authorsPath);
        public List<string> LoadSeries() => LoadList(_seriesPath);

        public void AddAuthor(string author)
        {
            AddItem(_authorsPath, author);
        }

        public void AddSeries(string series)
        {
            AddItem(_seriesPath, series);
        }

        private void EnsureFileExists(string path, string defaultContent)
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, defaultContent);
            }
        }

        private List<string> LoadList(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        private void AddItem(string path, string item)
        {
            lock (_lock)
            {
                var list = LoadList(path);
                if (!list.Contains(item, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(item);
                    var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(path, json);
                }
            }
        }
    }
}