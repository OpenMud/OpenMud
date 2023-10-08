namespace OpenMud.Mudpiler.Compiler.Asset;

public struct DMIMetaData
{
    private static readonly Direction[] DMIDirectionOrder =
    {
        Direction.South,
        Direction.North,
        Direction.East,
        Direction.West,
        Direction.SouthEast,
        Direction.SouthWest,
        Direction.NorthEast,
        Direction.NorthWest
    };

    public int height;
    public int width;
    public DMIState[] states;

    private Dictionary<Direction, List<FrameInfo>> ExtractFrameInformation(DMIState state, int imageWidth,
        int imageHeight, int originIdx)
    {
        Rectangle GetBounds(DMIMetaData md, int idx)
        {
            var xFrames = imageWidth / md.width;
            var yFrames = imageHeight / md.height;

            var xPos = idx % xFrames * md.width;
            var yPos = idx / xFrames * md.height;

            return new Rectangle(xPos, yPos, md.width, md.height);
        }

        ;

        var dirMap = new Dictionary<Direction, List<FrameInfo>>();

        for (var i = 0; i < state.frames; i++)
        for (var w = 0; w < state.directions; w++)
        {
            var dirName = DMIDirectionOrder[w];

            if (!dirMap.ContainsKey(dirName))
                dirMap.Add(dirName, new List<FrameInfo>());

            var srcIdx = originIdx + state.directions * i + w;
            var bounds = GetBounds(this, srcIdx);
            var delay = state.delays[i] * 100;

            dirMap[dirName].Add(new FrameInfo { Delay = delay, Bounds = bounds });
        }

        return dirMap;
    }

    public Dictionary<DMIState, Dictionary<Direction, List<FrameInfo>>> ExtractFrameInformation(int imageWidth,
        int imageHeight)
    {
        Dictionary<DMIState, Dictionary<Direction, List<FrameInfo>>> result = new();

        var origin = 0;
        foreach (var state in states)
        {
            var action_name = state.name;

            //Each element is a list of frames for the respective direction. Starting N, NE, E, SE, S, SW, W, NW 
            var frames = ExtractFrameInformation(state, imageWidth, imageHeight, origin);
            origin += frames.Sum(x => x.Value.Count);

            result[state] = frames;
        }

        return result;
    }
}