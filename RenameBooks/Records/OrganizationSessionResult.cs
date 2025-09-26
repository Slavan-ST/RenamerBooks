using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Records
{
    public record OrganizationSessionResult(
        IReadOnlyList<OrganizationResult> OrganizedBooks,
        int UnsupportedFilesCount,
        string OrganizedRoot,
        string NotOrganizedRoot,
        string LogSummary
    );
}
