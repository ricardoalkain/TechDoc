using System.IO.Enumeration;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TechDoc.Data.Config;
using TechDoc.Data.Exceptions;
using TechDoc.Data.Models;

namespace TechDoc.Data
{
    internal class FileDocumentManager : IDocumentManager
    {
        private static readonly object _lockIndex = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private const string BIN_FOLDER = ".deleted";
        private const string INDEX_FILE = ".index.json";

        private readonly DocManagerConfig _config;
        private readonly IEnumerable<char> _invalidChars;
        private readonly string _indexFileName;
        private readonly string _binFolderName;
        private readonly string _rootFolderName;

        public FileDocumentManager(IOptions<DocManagerConfig> configuration)
        {
            _config = configuration.Value;
            _invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars());
            _indexFileName = Path.Combine(_config.Location, INDEX_FILE);

            _rootFolderName = Path.TrimEndingDirectorySeparator(_config.Location).TrimEnd(Path.DirectorySeparatorChar);
            _binFolderName = Path.Combine(_rootFolderName, BIN_FOLDER).TrimEnd(Path.DirectorySeparatorChar);

            Init();
        }




        public async Task<Document> Get(Guid id)
        {
            var index = await LoadIndex();
            if (index.TryGetValue(id, out var doc))
            {
                return doc;
            }

            throw new DocumentNotFoundException(id);
        }

        public async Task<Document> Create(string folder, string name, string type, string content, bool overwrite = false)
        {
            CheckName(folder);
            CheckName(name);

            var fullPath = GetFullPath(folder, name, type);
            var fi = new FileInfo(fullPath);
            if (fi.Exists && (fi.Length > 0 || !overwrite))
                throw new ExistingDocumentException(fullPath?.Substring(_rootFolderName.Length));

            var doc = new DocumentFile
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = type,
                Folder = folder,
                CreatedOn = DateTime.UtcNow,
                LastSavedOn = DateTime.UtcNow,
                DeletedOn = null,
                Size = 0,
                FullPath = fullPath,
            };

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.Delete(fullPath);

            if (string.IsNullOrEmpty(content))
            {
                File.Create(fullPath).Close();
            }
            else
            {
                await File.WriteAllTextAsync(fullPath, content);
            }

            var index = await LoadIndex();
            index.Add(doc.Id, doc);
            await SaveIndex(index);

            return doc;
        }

        public async Task<string> LoadContent(Guid id)
        {
            var doc = await Get(id) as DocumentFile;
            var content = await File.ReadAllTextAsync(doc.FullPath);
            return content;
        }

        public async Task SaveContent(Guid id, string content)
        {
            var doc = await Get(id) as DocumentFile;
            await File.WriteAllTextAsync(doc.FullPath, content);
        }

        public async Task Delete(Guid id)
        {
            var index = await LoadIndex();
            if (!index.TryGetValue(id, out var doc))
                throw new DocumentNotFoundException(id);

            if (doc.DeletedOn.HasValue)
                return;

            var oldPath = doc.FullPath;
            doc.FullPath = doc.FullPath.Replace(_rootFolderName, _binFolderName) + "@" + doc.DeletedOn.Value.ToString("yyyyMMddHHmmssfffffff");
            doc.DeletedOn = DateTime.UtcNow;

            Directory.CreateDirectory(Path.GetDirectoryName(doc.FullPath));
            File.Move(oldPath, doc.FullPath);

            index[id] = doc;
            await SaveIndex(index);
        }

        public async Task Undelete(Guid id)
        {
            var index = await LoadIndex();
            if (!index.TryGetValue(id, out var doc))
                throw new DocumentNotFoundException(id);

            if (!doc.DeletedOn.HasValue)
                return;

            var oldPath = doc.FullPath;
            var rx = new Regex(@"(.*)@\d*", RegexOptions.IgnoreCase).Match(oldPath);
            if (!rx.Success)
                throw new Exception($"Document internal path is not in the correct format: {oldPath}");

            oldPath = rx.Groups[0].Value.Replace(_binFolderName, _rootFolderName);
            doc.DeletedOn = null;

            Directory.CreateDirectory(Path.GetDirectoryName(oldPath));
            File.Move(doc.FullPath, oldPath, false);

            doc.FullPath = oldPath;

            index[id] = doc;
            await SaveIndex(index);
        }

        public async Task Rename(Guid id, string newName)
        {
            CheckName(newName);

            var index = await LoadIndex();
            if (!index.TryGetValue(id, out var doc))
                throw new DocumentNotFoundException(id);

            doc.Name = newName;
            index[id] = doc;
            await SaveIndex(index);
        }

        public async Task Move(Guid id, string newFolder)
        {
            CheckName(newFolder);

            var index = await LoadIndex();
            if (!index.TryGetValue(id, out var doc))
                throw new DocumentNotFoundException(id);

            var oldPath = doc.FullPath;
            doc.Folder = newFolder;
            doc.FullPath = GetFullPath(doc.Folder, doc.Name, doc.Type);

            Directory.CreateDirectory(Path.GetDirectoryName(doc.FullPath));
            File.Move(oldPath, doc.FullPath, false);

            index[id] = doc;
            await SaveIndex(index);
        }

        public async Task<Document> CreateCopy(Guid id, string copyName = null)
        {
            var index = await LoadIndex();
            if (!index.TryGetValue(id, out var doc))
                throw new DocumentNotFoundException(id);

            var newName = copyName ?? doc.Name + " (copy)";
            CheckName(newName);
            var content = await LoadContent(id);

            return await Create(doc.Folder, newName, doc.Type, content);
        }


        public async Task<IEnumerable<Document>> Search(string folder, string pattern, bool includeDeleted = false)
        {
            var wildcards = new[] { '*', '?' };
            var index = (await LoadIndex())
                .Select(kv => kv.Value)
                .Where(d => includeDeleted || !d.DeletedOn.HasValue);

            if (!string.IsNullOrEmpty(folder))
            {
                index = folder.IndexOfAny(wildcards) < 0
                    ? index.Where(d => d.Folder.Contains(folder, StringComparison.InvariantCultureIgnoreCase))
                    : index.Where(d => FileSystemName.MatchesSimpleExpression(d.Folder, folder));
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                index = pattern.IndexOfAny(wildcards) < 0
                    ? index.Where(d => d.Name.Contains(pattern, StringComparison.InvariantCultureIgnoreCase))
                    : index.Where(d => FileSystemName.MatchesSimpleExpression(d.Name, pattern));
            }

            return index.ToList();
        }




        #region Private

        private void Init()
        {
            Directory.CreateDirectory(_rootFolderName);

            lock (_lockIndex)
            {
                if (!File.Exists(_indexFileName))
                {
                    RebuildIndex().Wait();
                    var attr = File.GetAttributes(_indexFileName);
                    File.SetAttributes(_indexFileName, attr | FileAttributes.Hidden);
                }
            }

            if (!Directory.Exists(_binFolderName))
            {
                var di = Directory.CreateDirectory(_binFolderName);
                di.Attributes |= FileAttributes.Hidden;
            }
        }

        private async Task RebuildIndex()
        {
            var files = Directory.GetFiles(_rootFolderName, "*", SearchOption.AllDirectories);
            var index = files
                .Where(f => !f.StartsWith('.'))
                .Select(f =>
                {
                    var doc = GetDocumentDataFromFile(f);
                    doc.Id = Guid.NewGuid();
                    return doc;
                })
                .ToDictionary(k => k.Id, v => v);

            await SaveIndex(index);
        }

        private async Task<Dictionary<Guid, DocumentFile>> LoadIndex()
        {
            var json = await File.ReadAllTextAsync(_indexFileName);
            return JsonSerializer.Deserialize<Dictionary<Guid, DocumentFile>>(json, _jsonOptions);
        }

        private async Task SaveIndex(Dictionary<Guid, DocumentFile> newIndex)
        {
            var json = JsonSerializer.Serialize(newIndex, _jsonOptions);
            await File.WriteAllTextAsync(_indexFileName, json);
        }

        private string GetFullPath(string folder, string name, string type)
        {
            return Path.ChangeExtension(Path.Combine(_rootFolderName, folder, name), type);
        }

        private void CheckName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Any(c => _invalidChars.Contains(c)))
                throw new InvalidFileNameException(name);
        }

        private DocumentFile GetDocumentDataFromFile(string fullPath)
        {
            var info = new FileInfo(fullPath);
            return new DocumentFile
            {
                Name = Path.GetFileNameWithoutExtension(fullPath),
                Folder = Path.GetDirectoryName(fullPath)[_rootFolderName.Length..],
                Type = Path.GetExtension(fullPath),
                Size = (int)info.Length / 2,
                FullPath = fullPath,
                CreatedOn = info.CreationTimeUtc,
                LastSavedOn = info.LastWriteTimeUtc,
                DeletedOn = fullPath.StartsWith(_binFolderName) ? info.LastAccessTimeUtc : null,
            };
        }

        #endregion
    }
}
