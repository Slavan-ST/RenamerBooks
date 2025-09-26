using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Records
{
    public record BookMetadata(
        string? Title,
        IReadOnlyList<Author> Authors, 
        string? SeriesName,
        int? SeriesNumber
    );
}
