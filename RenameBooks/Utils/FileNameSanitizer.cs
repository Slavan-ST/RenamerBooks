using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Utils
{
    public class FileNameSanitizer : IFileNameSanitizer
    {
        public string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "unknown";

            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", input.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
                       .Trim('_')
                       .Replace("__", "_");
        }
    }
}
