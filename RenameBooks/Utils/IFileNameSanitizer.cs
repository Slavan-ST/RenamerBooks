using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Utils
{
    public interface IFileNameSanitizer
    {
        string Sanitize(string input);
    }
}
