public class PipeWriterStream : Stream
{
    private readonly PipeWriter _writer;

    public PipeWriterStream(PipeWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // Consider using Memory<byte>.Slice for more efficient offset handling
        var memory = _writer.GetMemory(count);
        new ReadOnlySpan<byte>(buffer, offset, count).CopyTo(memory.Span);
        _writer.Advance(count);

        // Here we handle backpressure by flushing the writer and checking the result
        var result = await _writer.FlushAsync(cancellationToken);
        if (result.IsCompleted)
        {
            // Handle the case where the PipeWriter is completed,
            // which might indicate that the reader side is done or there was an error.
        }
    }

    #region Proper Stream Implementation

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException("Length is not supported.");
    public override long Position
    {
        get => throw new NotSupportedException("Position is not supported.");
        set => throw new NotSupportedException("Setting position is not supported.");
    }
    
    public override void Flush() => _writer.FlushAsync().GetAwaiter().GetResult();
    
    // Since this stream is write-only, Read is not supported and throws an exception.
    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Read is not supported.");

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException("Seek is not supported.");

    public override void SetLength(long value) =>
        throw new NotSupportedException("SetLength is not supported.");

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Optional: Consider how you want to handle disposing, like completing the writer.
            _writer.Complete();
        }

        base.Dispose(disposing);
    }
}
