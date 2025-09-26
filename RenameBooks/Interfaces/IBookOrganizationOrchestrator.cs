using RenameBooks.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface IBookOrganizationOrchestrator
    {
        Task<OrganizationSessionResult> OrganizeAsync(string rootFolder, bool isRecursive);
    }
}
