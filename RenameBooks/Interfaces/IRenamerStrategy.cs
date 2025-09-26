using RenameBooks.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface IRenamerStrategy
    {
        bool CanHandle(string filePath);
        BookMetadata? ExtractMetadata(string filePath);
        public IEnumerable<string> GetSupportedExtensions();
    }

}
