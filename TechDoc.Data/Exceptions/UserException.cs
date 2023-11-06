namespace TechDoc.Data.Exceptions
{
    /// <summary>
    /// Used to identify exceptions thrown by bad requests
    /// </summary>
    public class UserException : Exception
    {
        public UserException()
        {
        }

        public UserException(string message) : base(message)
        {
        }

        public UserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
