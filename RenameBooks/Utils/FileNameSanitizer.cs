using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RenameBooks.Utils
{
    public class FileNameSanitizer : IFileNameSanitizer
    {
        private const string DefaultFileName = "без_названия";
        private static readonly HashSet<char> InvalidCharSet = new(Path.GetInvalidFileNameChars());
        private static readonly char[] TrimmableChars = { '.', ' ', '_' };

        public string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return DefaultFileName;

            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                sb.Append(InvalidCharSet.Contains(c) ? '_' : c);
            }

            var sanitized = sb.ToString().Trim(TrimmableChars);
            return string.IsNullOrEmpty(sanitized) ? DefaultFileName : sanitized;
        }
    }
}