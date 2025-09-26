using System.Collections.Generic;

namespace RenameBooks.Records
{
    public record OrganizationResult(
        string OriginalFilePath,
        string Title,
        IReadOnlyList<string> CreatedFilePaths // все созданные копии
    );
}