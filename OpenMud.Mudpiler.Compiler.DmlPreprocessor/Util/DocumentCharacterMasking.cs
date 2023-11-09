namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public class DocumentCharacterMasking
{
    private readonly List<bool> maskedOut;

    public DocumentCharacterMasking(int size, bool isMaseked)
    {
        maskedOut = new List<bool>();
        Append(size, isMaseked);
    }

    public DocumentCharacterMasking()
    {
        maskedOut = new List<bool>();
    }

    public void Append(DocumentCharacterMasking m)
    {
        maskedOut.AddRange(m.MaskedOut);
    }

    public void Append(int length, bool masked = false)
    {
        var insert = Enumerable.Range(0, length).Select(_ => masked);

        maskedOut.AddRange(insert);
    }

    public bool Accept(int start, int length, bool allowResource = false)
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