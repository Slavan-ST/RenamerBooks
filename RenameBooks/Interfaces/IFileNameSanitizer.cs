using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface IFileNameSanitizer
    {
        string Sanitize(string input);
    }
}
