namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public interface IImmutableDocumentCharacterMasking
{
    IEnumerable<bool> MaskedOut { get; }
    public int Length { get; }
    bool Accept(int start, int length);
}

public class ConcatDocumentCharacterMasking : IImmutableDocumentCharacterMasking
{
    private IImmutableDocumentCharacterMasking[] maskings;
    public IEnumerable<bool> MaskedOut => maskings.Select(x => x.MaskedOut).Aggregate((a, b) => a.Concat(b));
    public int Length => maskings.Select(x => x.Length).Aggregate((a, b) => a + b);

    public ConcatDocumentCharacterMasking(IEnumerable<IImmutableDocumentCharacterMasking> src)
    {
        maskings = src.ToArray();
    }

    public bool Accept(int start, int length)
    {
        var maskingOffset = 0;
        int readLen = length;
        foreach (var m in maskings) {
            if (readLen <= 0)
                break;

            int relativeOffset = start - maskingOffset;
            var contains = relativeOffset < m.Length;
            maskingOffset += m.Length;

            if (!contains)
                continue;

            var readable = Math.Min(m.Length - relativeOffset, readLen);
            readLen -= readable;

            if (!m.Accept(relativeOffset, readable))
                return false;
        }

        return true;
    }
}

public class DocumentCharacterMasking : IImmutableDocumentCharacterMasking
{
    private readonly List<bool> maskedOut;
    public int Length => maskedOut.Count;

    public DocumentCharacterMasking(int size, bool isMaseked)
    {
        maskedOut = new List<bool>(size);
        Append(size, isMaseked);
    }

    public DocumentCharacterMasking()
    {
        maskedOut = new List<bool>();
    }

    public void Append(IImmutableDocumentCharacterMasking m)
    {
        maskedOut.AddRange(m.MaskedOut);
    }

    public void Append(int length, bool masked = false)
    {
        var insert = Enumerable.Range(0, length).Select(_ => masked);
        maskedOut.Capacity = maskedOut.Count + length;
        maskedOut.AddRange(insert);
    }

    public bool Accept(int start, int length)
    {
        return Enumerable.Range(start, length)
            .All(i => !maskedOut[i]);
    }

    public void Replace(int start, int replaceLength, int insertLength, bool isMasekdOut = false)
    {
        int delta = insertLength - replaceLength;

        for (var i = 0; i < Math.Abs(delta); i++)
        {
            if (delta > 0)
            {
                maskedOut.Insert(start, false);
            }
            else
            {
                maskedOut.RemoveAt(start);
            }
        }

        for (var i = 0; i < insertLength; i++)
        {
            maskedOut[start + i] = isMasekdOut;
        }
    }

    public IEnumerable<bool> MaskedOut
    {
        get
        {
            for (var i = 0; i < maskedOut.Count; i++)
                yield return maskedOut[i];
        }
    }
}