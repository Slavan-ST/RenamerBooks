using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Strategies
{

    public class Fb2RenamerStrategy : IRenamerStrategy
    {
        public bool CanHandle(string filePath)
        {
            return filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase);
        }

        public string ExtractTitle(string filePath)
        {
            // ... ваша логика из предыдущего примера ...
            // (чтение XML, обработка ZIP и т.д.)
            // Возвращает null, если не удалось
        }
    }
}
