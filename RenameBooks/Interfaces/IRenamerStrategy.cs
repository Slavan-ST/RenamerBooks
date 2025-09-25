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
        string? ExtractTitle(string filePath);
        (string? SeriesName, int? SeriesNumber) ExtractSeriesInfo(string filePath);
        string? ExtractAuthor(string filePath);
    }
}
