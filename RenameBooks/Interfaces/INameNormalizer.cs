using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface INameNormalizer
    {
        string NormalizeAuthor(string rawAuthor);
        string NormalizeSeries(string rawSeries);
    }
}
