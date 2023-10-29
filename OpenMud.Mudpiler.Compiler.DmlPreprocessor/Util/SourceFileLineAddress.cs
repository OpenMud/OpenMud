namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public readonly struct SourceFileLineAddress
{
    public readonly int Line;
    public readonly string FileName;

    public SourceFileLineAddress(int line, string fileName)
    {
        Line = line;
        FileName = fileName;

        if (line <= 0)
            throw new ArgumentOutOfRangeException("line numbers start from 1.");
    }
}