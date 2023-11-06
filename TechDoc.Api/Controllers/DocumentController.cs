using Microsoft.AspNetCore.Mvc;
using TechDoc.Data;
using TechDoc.Data.Exceptions;

namespace TechDoc.Api.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentController : ApiControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IDocumentManager _documentManager;

        public DocumentController(
            ILogger<DocumentController> logger,
            IDocumentManager documentManager)
        {
            _logger = logger;
            _documentManager = documentManager;
        }

        /// <summary>
        /// Search for documents.
        /// </summary>
        /// <remarks>
        /// Both <paramref name="folder"/> and <paramref name="name"/> parameters allow wildcards.
        /// For example: "mydocs/ref*", "*.pdf"...
        /// </remarks>
        /// <param name="folder">Folder where to search for documents. If not provided, search in all folders.</param>
        /// <param name="name">Document name to search for.</param>
        /// <returns>List of found documents.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Search(string folder, string name)
        {
            return GetResponse(async () => await _documentManager.Search(folder, name));
        }

        /// <summary>
        /// Retrieves information about a document.
        /// </summary>
        /// <param name="id">Document id</param>
        /// <returns>Document information</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(string id)
        {
            return await GetResponse(async () => await _documentManager.Get(Guid.Parse(id)));
        }

        /// <summary>
        /// Retrieves the current content of a document.
        /// </summary>
        /// <param name="id">Document id.</param>
        /// <returns>Raw content of the document</returns>
        [HttpGet("{id}/content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetContent(string id)
        {
            return await GetResponse(async () => await _documentManager.LoadContent(Guid.Parse(id)));
        }

        /// <summary>
        /// Updates the content of a document.
        /// </summary>
        /// <param name="id">Document id</param>
        /// <param name="content">New raw content of the document</param>
        [HttpPost("{id}/content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveContent(string id, [FromBody] string content)
        {
            return await GetResponse(async () => await _documentManager.SaveContent(Guid.Parse(id), content));
        }

        /// <summary>
        /// Marks a document as deleted.
        /// </summary>
        /// <param name="id">Document id</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            return await GetResponse(async () => await _documentManager.Delete(Guid.Parse(id)));
        }

        /// <summary>
        /// Restores a previously deleted document.
        /// </summary>
        /// <param name="id">Document id</param>
        [HttpPut("{id}/undelete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Undelete(string id)
        {
            return await GetResponse(async () => await _documentManager.Undelete(Guid.Parse(id)));
        }

        /// <summary>
        /// Changes the name of a document.
        /// </summary>
        /// <param name="id">Document id.</param>
        /// <param name="newName">Document's new name.</param>
        [HttpPut("{id}/rename")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Rename(string id, string newName)
        {
            return await GetResponse(async () => await _documentManager.Rename(Guid.Parse(id), newName));
        }

        /// <summary>
        /// Moves a document to another folder.
        /// </summary>
        /// <param name="id">Document id.</param>
        /// <param name="toFolder">Folder where the document will be moved in.</param>
        [HttpPost("{id}/move")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Move(string id, string toFolder)
        {
            return await GetResponse(async () => await _documentManager.Move(Guid.Parse(id), toFolder));
        }

        /// <summary>
        /// Creates a new document copying all data (including content) from another document.
        /// </summary>
        /// <param name="id">Document id</param>
        /// <param name="name">Optional name for the new document.</param>
        /// <returns>Newly created document.</returns>
        [HttpPost("{id}/create-copy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Duplicate(string id, string name)
        {
            try
            {
                var newDoc = await _documentManager.CreateCopy(Guid.Parse(id), name);
                return CreatedAtAction(nameof(GetById), new { newDoc.Id });
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Creates a new document.
        /// </summary>
        /// <param name="folder">Folder where the document will be created.</param>
        /// <param name="name">Name of the new document.</param>
        /// <param name="type">Document type (extension)</param>
        /// <param name="content">Optional content to initialize the document.</param>
        /// <returns>Id of the new document.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(string folder, string name, string type, [FromBody] string content)
        {
            try
            {
                var newDoc = await _documentManager.Create(folder, name, type, content);
                return CreatedAtAction(nameof(GetById), new { newDoc.Id });
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}