namespace OpenMud.Mudpiler.Compiler.DmlPreprocessor.Util;

public class DocumentCharacterMasking
{
    private readonly List<bool> commentsAndStrings;
    private readonly List<bool> resources;

    public DocumentCharacterMasking(string document)
    {
        var (srcCommentsAndStrings, srcResources) = Blackout.CreateBitmaps(document);

        commentsAndStrings = new List<bool>(srcCommentsAndStrings);
        resources = new List<bool>(srcResources);
    }

    public void Insert(int start, int length, bool isCommentOrString = false, bool isResource = false)
    {
        Replace(start, 0, length, isCommentOrString, isResource);
    }

    public bool Accept(int start, int length, bool allowResource = false)
    {
        return Enumerable.Range(start, length)
            .All(i => !commentsAndStrings[i] && (allowResource || !resources[i]));
    }

    public void Replace(int start, int replaceLength, int insertLength, bool isCommentOrString = false,
        bool isResource = false)
    {
        int delta = insertLength - replaceLength;

        for (var i = 0; i < Math.Abs(delta); i++)
        {
            if (delta > 0)
            {
                commentsAndStrings.Insert(start, false);
                resources.Insert(start, false);
            }
            else
            {
                commentsAndStrings.RemoveAt(start);
                resources.RemoveAt(start);
            }
        }

        for (var i = 0; i < insertLength; i++)
        {
            commentsAndStrings[start + i] = isCommentOrString;
            resources[start + i] = isResource;
        }
    }

    public IEnumerable<bool> CommentsAndStrings
    {
        get
        {
            for (var i = 0; i < commentsAndStrings.Count; i++)
                yield return commentsAndStrings[i];
        }
    }

    public IEnumerable<bool> Resources
    {
        get
        {
            for (var i = 0; i < resources.Count; i++)
                yield return resources[i];
        }
    }
}