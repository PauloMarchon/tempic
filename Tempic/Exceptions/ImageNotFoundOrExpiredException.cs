namespace Tempic.Exceptions
{
    public class ImageNotFoundOrExpiredException : Exception
    {
        public ImageNotFoundOrExpiredException()
            : base("The image was not found or has expired.")
        {
        }

        public ImageNotFoundOrExpiredException(string message)
            : base(message)
        {
        }

        public ImageNotFoundOrExpiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
