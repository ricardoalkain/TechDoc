using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechDoc.Data.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        /// <summary>
        /// File name without folder and extension
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Document logical folder
        /// </summary>
        public string Folder { get; set; }
        /// <summary>
        /// Document content type
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// When file was first saved
        /// </summary>
        public DateTime CreatedOn { get; set; }
        /// <summary>
        /// Last time file was saved
        /// </summary>
        public DateTime LastSavedOn { get; set; }
        /// <summary>
        /// File has been sent to trash bin
        /// </summary>
        public DateTime? DeletedOn { get; set; }
        /// <summary>
        /// Content size in characters
        /// </summary>
        public int Size { get; set; }
    }
}
