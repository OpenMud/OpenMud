using System.Collections;
using System.Collections.Immutable;
using Microsoft.VisualBasic;
using static System.Net.Mime.MediaTypeNames;

namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public readonly struct SourceFileLine
{
    public readonly SourceFileLineAddress Address;
    public readonly int Index;

    public SourceFileLine(SourceFileLineAddress address, int index)
    {
        Address = address;
        Index = index;
    }

    public SourceFileLine Offset(int deltaOffset)
    {
        return new SourceFileLine(Address, Index + deltaOffset);
    }
}