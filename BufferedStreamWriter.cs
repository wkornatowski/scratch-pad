using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class BufferedStreamWriter : TextWriter
{
    private readonly Stream _stream;
    private readonly Encoder _encoder;
    private readonly byte[] _buffer;
    private int _bufferIndex;
    private const int DefaultBufferSize = 4096; // Customize the buffer size as needed

    public BufferedStreamWriter(Stream stream, Encoding encoding = null, int bufferSize = DefaultBufferSize)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _encoder = (encoding ?? Encoding.UTF8).GetEncoder();
        _buffer = new byte[bufferSize];
        _bufferIndex = 0;
    }

    public override Encoding Encoding => _encoder.Encoding;

    public override void Write(char value)
    {
        Write(new ReadOnlySpan<char>(new[] { value }));
    }

    public override void Write(char[] buffer, int index, int count)
    {
        Write(new ReadOnlySpan<char>(buffer, index, count));
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        if (buffer.IsEmpty) return;

        int charsUsed;
        int bytesUsed;
        bool completed;

        do
        {
            _encoder.Convert(buffer, _buffer.AsSpan(_bufferIndex), flush: false, out charsUsed, out bytesUsed, out completed);
            _bufferIndex += bytesUsed;
            if (_bufferIndex == _buffer.Length)
            {
                FlushBuffer();
            }
            buffer = buffer.Slice(charsUsed);
        } while (!completed);
    }

    private void FlushBuffer()
    {
        if (_bufferIndex > 0)
        {
            _stream.Write(_buffer, 0, _bufferIndex);
            _bufferIndex = 0;
        }
    }

    public override void Flush()
    {
        FlushBuffer();
        _stream.Flush();
    }

    protected override async Task WriteAsync(ReadOnlyMemory<char> buffer, bool appendNewLine)
    {
        if (buffer.IsEmpty) return;

        Encoder encoder = _encoder;
        byte[] byteBuffer = _buffer;
        int byteIndex = _bufferIndex;

        encoder.Convert(buffer.Span, byteBuffer.AsSpan(byteIndex), flush: false, out int charsUsed, out int bytesUsed, out bool completed);
        byteIndex += bytesUsed;
        if (byteIndex == byteBuffer.Length)
        {
            await _stream.WriteAsync(byteBuffer, 0, byteIndex);
            byteIndex = 0;
        }

        _bufferIndex = byteIndex;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
            _stream.Dispose();
        }
        base.Dispose(disposing);
    }
}
