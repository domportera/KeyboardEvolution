using System.Text;

namespace ThumbKey;

public class TextAndConsoleWriter : TextWriter
{
    readonly TextWriter _standardOutput;
    readonly StreamWriter _textWriter;

    public TextAndConsoleWriter(string path, bool append)
    {
        _standardOutput = Console.Out;
        var encoding = _standardOutput.Encoding;
        Encoding = encoding;

        if (!append && File.Exists(path))
        {
            File.Delete(path);
        }

        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }

        FileStream fileStream = new(path, FileMode.Append);
        _textWriter = new StreamWriter(fileStream, encoding);
        _textWriter.AutoFlush = false;
    }

    public override void Write(string value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
        _textWriter.Flush();
    }

    public override void WriteLine(string? value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
        _textWriter.Flush();
    }

    public override void WriteLine()
    {
        _standardOutput.WriteLine();
        _textWriter.WriteLine();
        _textWriter.Flush();
    }

    public override void WriteLine(object? value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(string format, object? arg0)
    {
        _standardOutput.WriteLine(format, arg0);
        _textWriter.WriteLine(format, arg0);
        _textWriter.Flush();
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        _standardOutput.WriteLine(format, arg0, arg1);
        _textWriter.WriteLine(format, arg0, arg1);
        _textWriter.Flush();
    }

    public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
    {
        _standardOutput.WriteLine(format, arg0, arg1, arg2);
        _textWriter.WriteLine(format, arg0, arg1, arg2);
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        _standardOutput.WriteLine(format, arg);
        _textWriter.WriteLine(format, arg);
    }

    public override void Write(char value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(bool value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(char[] buffer)
    {
        _standardOutput.Write(buffer);
        _textWriter.Write(buffer);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        _standardOutput.Write(buffer, index, count);
        _textWriter.Write(buffer, index, count);
    }

    public override void Write(double value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(float value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(int value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(long value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(object? value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(string format, object? arg0)
    {
        _standardOutput.Write(format, arg0);
        _textWriter.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        _standardOutput.Write(format, arg0, arg1);
        _textWriter.Write(format, arg0, arg1);
    }

    public override void Write(string format, object? arg0, object? arg1, object? arg2)
    {
        _standardOutput.Write(format, arg0, arg1, arg2);
        _textWriter.Write(format, arg0, arg1, arg2);
    }

    public override void Write(string format, params object?[] arg)
    {
        _standardOutput.Write(format, arg);
        _textWriter.Write(format, arg);
    }

    public override void Write(uint value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void Write(ulong value)
    {
        _standardOutput.Write(value);
        _textWriter.Write(value);
    }

    public override void WriteLine(bool value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(char value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(char[] buffer)
    {
        _standardOutput.WriteLine(buffer);
        _textWriter.WriteLine(buffer);
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        _standardOutput.WriteLine(buffer, index, count);
        _textWriter.WriteLine(buffer, index, count);
    }

    public override void WriteLine(double value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(float value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(int value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(long value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(uint value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override void WriteLine(ulong value)
    {
        _standardOutput.WriteLine(value);
        _textWriter.WriteLine(value);
    }

    public override Encoding Encoding { get; }
}