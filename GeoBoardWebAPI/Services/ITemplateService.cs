using System.Threading.Tasks;

namespace GeoBoardWebAPI.Services
{
    public interface ITemplateService
    {
        string RenderTemplate<T>(string templatePath, T viewModel, bool isFullPathProvided = false);
        Task<string> RenderTemplateAsync<T>(string templatePath, T viewModel, bool isFullPathProvided = false);
    }
}
