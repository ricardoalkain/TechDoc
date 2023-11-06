using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TechDoc.Data.Exceptions
{
    public class DocumentNotFoundException : UserException
    {
        public DocumentNotFoundException()
        {
        }

        public DocumentNotFoundException(Guid fileId) : this(fileId.ToString(), null)
        {
        }

        public DocumentNotFoundException(Guid fileId, Exception innerException) : this(fileId.ToString(), innerException)
        {
        }

        public DocumentNotFoundException(string fileName) : this(fileName, null)
        {
        }

        public DocumentNotFoundException(string fileName, Exception? innerException) :
            base($"Document \"{fileName}\" not found!", innerException)
        {
        }
    }

    public class ExistingDocumentException : UserException
    {
        public ExistingDocumentException()
        {
        }

        public ExistingDocumentException(Guid fileId) : this(fileId.ToString(), null)
        {
        }

        public ExistingDocumentException(Guid fileId, Exception innerException) : this(fileId.ToString(), innerException)
        {
        }

        public ExistingDocumentException(string fileName) : this(fileName, null)
        {
        }

        public ExistingDocumentException(string fileName, Exception? innerException) :
            base($"A document named \"{fileName}\" already exists!", innerException)
        {
        }
    }

    public class InvalidFileNameException : UserException
    {
        public InvalidFileNameException()
        {
        }

        public InvalidFileNameException(string fileName) : this(fileName, null)
        {
        }

        public InvalidFileNameException(string fileName, Exception? innerException) :
            base($"\"{fileName}\" is not a valid file name!", innerException)
        {
        }
    }
}
