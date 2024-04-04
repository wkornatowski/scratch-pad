const int chunkSize = 32 * 1024 * 1024; // 32MB
long totalBytesRead = 0;
List<byte> currentChunk = new List<byte>(chunkSize);

while (true)
{
    ReadResult result = await reader.ReadAsync();
    ReadOnlySequence<byte> buffer = result.Buffer;

    foreach (var segment in buffer)
    {
        var segmentSpan = segment.Span;

        while (segmentSpan.Length > 0)
        {
            var spaceLeft = chunkSize - currentChunk.Count;
            var take = Math.Min(spaceLeft, segmentSpan.Length);

            currentChunk.AddRange(segmentSpan.Slice(0, take).ToArray());
            segmentSpan = segmentSpan.Slice(take);

            if (currentChunk.Count == chunkSize)
            {
                // Upload the chunk
                await UploadChunkAsync(currentChunk.ToArray(), totalBytesRead);
                totalBytesRead += currentChunk.Count;
                currentChunk.Clear();
            }
        }
    }

    // Indicate how much of the buffer has been consumed
    reader.AdvanceTo(buffer.End);

    if (result.IsCompleted)
    {
        if (currentChunk.Count > 0)
        {
            // Upload any remaining data as the final chunk
            await UploadChunkAsync(currentChunk.ToArray(), totalBytesRead);
            totalBytesRead += currentChunk.Count;
            currentChunk.Clear();
        }
        break;
    }
}
