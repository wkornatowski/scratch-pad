using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Buffers;

public static class CsvSerializer
{
    public static async Task WriteCsvAsync<T>(PipeWriter writer, IAsyncEnumerable<T> records)
    {
        // Write the header row.
        var header = GetHeaderRow<T>();
        await WriteToPipe(writer, header);

        // Write each record.
        await foreach (var record in records)
        {
            var line = SerializeRecordToCsvLine(record);
            await WriteToPipe(writer, line);
        }

        // Complete the writer once all records are processed.
        writer.Complete();
    }

    private static async Task WriteToPipe(PipeWriter writer, string data)
    {
        var encoded = Encoding.UTF8.GetBytes(data);
        var memory = writer.GetMemory(encoded.Length);
        encoded.CopyTo(memory);
        writer.Advance(encoded.Length);
        await writer.FlushAsync();
    }

    private static string SerializeRecordToCsvLine<T>(T record)
    {
        var properties = typeof(T).GetProperties();
        var line = new StringBuilder();
        
        foreach (var prop in properties)
        {
            if (line.Length > 0)
                line.Append(',');

            var value = prop.GetValue(record, null)?.ToString() ?? "";
            line.Append(EscapeForCsv(value));
        }

        line.AppendLine(); // End the line
        return line.ToString();
    }

    private static string GetHeaderRow<T>()
    {
        var properties = typeof(T).GetProperties();
        var header = string.Join(",", properties.Select(p => EscapeForCsv(p.Name)));
        header += Environment.NewLine; // End the header line
        return header;
    }

    private static string EscapeForCsv(string input)
    {
        if (input.Contains('"'))
        {
            input = input.Replace("\"", "\"\""); // Escape double quotes as per CSV convention.
        }

        if (input.Contains(',') || input.Contains('\n') || input.Contains('"'))
        {
            input = $"\"{input}\""; // Enclose in double quotes if necessary.
        }

        return input;
    }
}
