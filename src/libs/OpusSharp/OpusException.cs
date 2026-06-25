namespace OpusSharp;

/// <summary>
/// Exception thrown by Opus operations
/// </summary>
public class OpusException : Exception
{
    public int ErrorCode { get; }

    public OpusException(int errorCode) : base($"Opus error: {errorCode}")
    {
        ErrorCode = errorCode;
    }

    public OpusException(int errorCode, string context) : base($"Opus error: {errorCode} - {context}")
    {
        ErrorCode = errorCode;
    }
}
