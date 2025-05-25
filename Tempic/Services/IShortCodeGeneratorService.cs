namespace Tempic.Services
{
    public interface IShortCodeGeneratorService
    {
        Task<List<string>> GenerateUniqueShortCodeAsync();
    }
}
