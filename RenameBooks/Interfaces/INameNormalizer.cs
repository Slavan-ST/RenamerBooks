namespace RenameBooks.Interfaces
{
    public interface INameNormalizer
    {
        string NormalizeAuthor(string rawAuthor);
        string NormalizeSeries(string rawSeries);
    }
}
