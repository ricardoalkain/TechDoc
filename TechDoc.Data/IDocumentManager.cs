using TechDoc.Data.Models;

namespace TechDoc.Data
{
    public interface IDocumentManager
    {
        Task<Document> Get(Guid id);

        Task<Document> Create(string folder, string name, string type, string content, bool overwrite = false);

        Task Rename(Guid id, string newName);

        Task Move(Guid id, string newFolder);

        Task<Document> CreateCopy(Guid id, string copyName = null);

        Task Delete(Guid id);

        Task Undelete(Guid id);

        Task<string> LoadContent(Guid id);

        Task SaveContent(Guid id, string content);

        Task<IEnumerable<Document>> Search(string folder, string pattern, bool includeDeleted = false);
    }
}
