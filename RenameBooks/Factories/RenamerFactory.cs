using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Factories
{
    public class RenamerFactory
    {
        private readonly IEnumerable<IRenamerStrategy> _strategies;

        public RenamerFactory(IEnumerable<IRenamerStrategy> strategies)
        {
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        }

        public IRenamerStrategy GetStrategy(string filePath)
        {
            return _strategies.FirstOrDefault(s => s.CanHandle(filePath))
                   ?? throw new NotSupportedException($"No renamer strategy for file: {filePath}");
        }
        public IEnumerable<string> GetSupportedExtensions() => _strategies.SelectMany(s => s.GetSupportedExtensions());
    }
}
