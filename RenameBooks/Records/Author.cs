using System.Collections.Generic;

namespace RenameBooks.Records
{
    public record Author(
        string? FirstName,
        string? MiddleName,
        string? LastName,
        string? Nickname
    )
    {
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(LastName))
            {
                var givenNames = new List<string>();
                if (!string.IsNullOrEmpty(FirstName)) givenNames.Add(FirstName);
                if (!string.IsNullOrEmpty(MiddleName)) givenNames.Add(MiddleName);
                string given = string.Join(" ", givenNames);
                return string.IsNullOrEmpty(given) ? LastName : $"{given} {LastName}";
            }

            if (!string.IsNullOrEmpty(Nickname))
                return Nickname;

            if (!string.IsNullOrEmpty(FirstName))
                return FirstName;

            return "Неизвестный автор";
        }
    }
}