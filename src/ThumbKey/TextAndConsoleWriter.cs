using System.Text;

namespace ThumbKey;

public class TextAndConsoleWriter : TextWriter
{
    TextWriter standardOutput;
    StreamWriter textWriter;

    public TextAndConsoleWriter(string path, bool append)
    {
        standardOutput = Console.Out;
        Encoding = standardOutput.Encoding;

        if (!append && File.Exists(path))
        {
            File.Delete(path);
        }

        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }

        FileStream fileStream = new(path, FileMode.Append);
        textWriter = new StreamWriter(fileStream, Encoding);
        textWriter.AutoFlush = false;
    }

    public override void Write(string value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
        textWriter.Flush();
    }

    public override void WriteLine(string? value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
        textWriter.Flush();
    }

    public override void WriteLine()
    {
        standardOutput.WriteLine();
        textWriter.WriteLine();
        textWriter.Flush();
    }

    public override void WriteLine(object? value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(string format, object? arg0)
    {
        standardOutput.WriteLine(format, arg0);
        textWriter.WriteLine(format, arg0);
        textWriter.Flush();
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        standardOutput.WriteLine(format, arg0, arg1);
        textWriter.WriteLine(format, arg0, arg1);
        textWriter.Flush();
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        standardOutput.WriteLine(format, arg0, arg1, arg2);
        textWriter.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        standardOutput.WriteLine(format, arg);
        textWriter.WriteLine(format, arg);
    }

    public override void Write(char value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(bool value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(char[] buffer)
    {
        standardOutput.Write(buffer);
        textWriter.Write(buffer);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        standardOutput.Write(buffer, index, count);
        textWriter.Write(buffer, index, count);
    }

    public override void Write(double value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(float value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(int value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(long value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(object? value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(string format, object? arg0)
    {
        standardOutput.Write(format, arg0);
        textWriter.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        standardOutput.Write(format, arg0, arg1);
        textWriter.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        standardOutput.Write(format, arg0, arg1, arg2);
        textWriter.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object?[] arg)
    {
        standardOutput.Write(format, arg);
        textWriter.Write(format, arg);
    }

    public override void Write(uint value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void Write(ulong value)
    {
        standardOutput.Write(value);
        textWriter.Write(value);
    }

    public override void WriteLine(bool value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(char value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(char[] buffer)
    {
        standardOutput.WriteLine(buffer);
        textWriter.WriteLine(buffer);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        standardOutput.WriteLine(buffer, index, count);
        textWriter.WriteLine(buffer, index, count);
    }

    public override void WriteLine(double value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(long value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(uint value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override void WriteLine(ulong value)
    {
        standardOutput.WriteLine(value);
        textWriter.WriteLine(value);
    }

    public override Encoding Encoding { get; }
}