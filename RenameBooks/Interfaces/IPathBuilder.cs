using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface IPathBuilder
    {
        string BuildBookPath(
            string targetRoot,
            string author,
            string series,
            int? seriesNumber,
            string title,
            string extension);
    }
}
