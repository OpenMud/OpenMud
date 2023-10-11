using System.Diagnostics;
using System.Text;
using SixLabors.ImageSharp.Formats.Png;

namespace OpenMud.Mudpiler.Compiler.Asset;

public static class AssetCompiler
{
    private static DMIMetaData ParseMetaData(Image image)
    {
        var metaDataText = image.Metadata.GetFormatMetadata(PngFormat.Instance).TextData.Select(n => n.Value).Single();
        var statements = metaDataText.Split('\n', '\r').Select(x => x.Trim())
            .Where(x => x.Length > 0 && x.First() != '#').ToList();

        var e = new Stack<string>(statements.Reverse<string>());
        if (e.Pop() != "version = 4.0")
            throw new Exception("Unsupported DMI Version...");

        var header = new Dictionary<string, string>();
        var states = new List<Dictionary<string, string>>();

        var current = header;

        while (e.Any())
        {
            if (e.Peek().StartsWith("state"))
            {
                current = new Dictionary<string, string>();
                states.Add(current);
            }

            var s = e.Pop();
            var keys = s.Split('=').Select(x => x.Trim().Trim('"')).ToList();

            current[keys[0]] = keys[1];
        }


        var width = 32;
        var height = 32;

        if (header.TryGetValue("height", out var h))
            height = int.Parse(h);

        if (header.TryGetValue("width", out var w))
            height = int.Parse(w);

        return new DMIMetaData
        {
            width = width,
            height = height,
            states = states.Select(ParseState).ToArray()
        };
    }

    private static DMIState ParseState(Dictionary<string, string> dictionary)
    {
        var s = new DMIState();

        s.name = dictionary["state"];

        if (s.name.Length == 0)
            s.name = "_default";

        s.frames = int.Parse(dictionary["frames"]);
        s.rewind = 0;
        s.directions = 1;
        s.delays = Enumerable.Range(0, s.frames).Select(e => 1.0f).ToArray();

        if (dictionary.TryGetValue("delays", out var delayStr))
            s.delays = delayStr.Split(",").Select(x => float.Parse(x)).ToArray();

        if (dictionary.TryGetValue("rewind", out var rewindStr))
            s.rewind = int.Parse(rewindStr);

        if (dictionary.TryGetValue("dirs", out var directionsStr))
            s.directions = int.Parse(directionsStr);

        return s;
    }

    private static Dictionary<string, string> ProcessAudio(AudioConverter audioConverter, string src, string dst)
    {
        //I had a really bad headache when I wrote this code...

        var audioSourceFiles = Directory.GetFiles(src, "*.wav", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(src, "*.mid", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(src, "*.ogg", SearchOption.AllDirectories))
            .ToArray();

        Dictionary<string, string> soundDirectory = new();

        foreach (var s in audioSourceFiles)
        {
            var relativeFullPath = Path.GetRelativePath(src, s);
            var relativeFileName = relativeFullPath;

            var outputFullPath = Path.Join(dst, relativeFullPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath));

            if (relativeFullPath.ToLower().EndsWith(".mid"))
            {
                outputFullPath = outputFullPath.Substring(0, outputFullPath.Length - 4) + ".wav";
                relativeFileName = relativeFileName.Substring(0, relativeFileName.Length - 4) + ".wav";
                audioConverter.ConvertMidiToWav(s, outputFullPath);
            }
            else
            {
                File.Copy(s, Path.Join(dst, relativeFullPath), true);
            }

            soundDirectory[relativeFullPath] = relativeFileName;
        }

        return soundDirectory;
    }

    private static void Process(AudioConverter audioConverter, string src, string dst, string resourceDefinition)
    {
        var dmiSourceFiles = Directory.GetFiles(src, "*.dmi", SearchOption.AllDirectories);
        Dictionary<string, string> assetDirectory = new();
        Dictionary<string, string[]> animationsDirectory = new();

        foreach (var s in dmiSourceFiles)
        {
            var relativeFullPath = Path.GetRelativePath(src, s);
            var relativeFileName = relativeFullPath;

            if (relativeFileName.ToLower().EndsWith(".dmi"))
                relativeFileName = relativeFileName.Substring(0, relativeFileName.Length - 4);


            assetDirectory[relativeFullPath] = $"{relativeFileName}.json";

            var dest_png = Path.Join(dst, $".\\{relativeFileName}.png");
            var resource_name = Path.GetFileName(relativeFileName);
            using (var image = Image.Load(s))
            {
                var metaData =
                    ParseMetaData(
                        image); // image.Metadata.GetFormatMetadata(PngFormat.Instance).TextData.Select(n => n.Value);
                var animations = BuildAseprite(image, metaData, dest_png, resource_name);

                animationsDirectory[relativeFullPath] = animations;
            }
        }

        GenerateAssetIndex(assetDirectory, animationsDirectory, ProcessAudio(audioConverter, src, dst), resourceDefinition);

        //
    }

    private static void GenerateAssetIndex(Dictionary<string, string> assetDirectory,
        Dictionary<string, string[]> animationsDirectory, Dictionary<string, string> audioResources, string indexName)
    {
        assetDirectory = assetDirectory.ToDictionary(
            x => x.Key.Replace("\\", "\\\\"),
            x => "assets/" + x.Value.Replace("\\", "/")
        );

        animationsDirectory = animationsDirectory.ToDictionary(
            x => x.Key.Replace("\\", "\\\\"),
            x => x.Value.Select(v => v.Replace("\\", "\\\\")).ToArray()
        );

        audioResources = audioResources.ToDictionary(
            x => x.Key.Replace("\\", "\\\\"),
            x => "assets/" + x.Value.Replace("\\", "/")
        );

        var resourceDecl = string.Join("\n",
            assetDirectory.Select(r => $"  '{r.Key}': new AsepriteResource('{r.Value}'),"));
        var soundDecl = string.Join("\n", audioResources.Select(r => $"  '{r.Key}': new Sound('{r.Value}'),"));
        var animIdx = string.Join("\n",
            animationsDirectory.Select(a => $"  '{a.Key}': [{string.Join(',', a.Value.Select(v => $"'{v}'"))}],"));

        var idxContents = $@"
import {{ Loader, Sound }} from ""excalibur"";
import {{ AsepriteResource }} from ""@excaliburjs/plugin-aseprite"";

const GameIcons = {{
{resourceDecl}
}}

const GameIconAnimationsIndex = {{
{animIdx}
}}

const GameSounds = {{
{soundDecl}
}}


const GameResourceLoader = new Loader()
const allResources: any = {{...GameIcons, ...GameSounds}}
for (const res in allResources) {{
  GameResourceLoader.addResource(allResources[res])
}}

export {{ GameResourceLoader, GameIcons, GameSounds, GameIconAnimationsIndex }}

";

        File.WriteAllText(indexName, idxContents);
    }

    private static string[] BuildAseprite(Image image, DMIMetaData metaData, string dest_png, string resourceName)
    {
        var resourcePath = Path.GetDirectoryName(dest_png);
        var pngName = Path.GetFileName(dest_png);
        Directory.CreateDirectory(resourcePath);
        image.SaveAsPng(dest_png);

        var frameInfo = metaData.ExtractFrameInformation(image.Width, image.Height);

        var animDirJson = GenerateAsepriteJson(frameInfo, Path.GetFileName(dest_png), image.Width, image.Height,
            out var animations);
        File.WriteAllText(Path.Combine(resourcePath, $"{resourceName}.json"), animDirJson);

        return animations;
    }

    private static (string json, Dictionary<string, Tuple<int, int>> animations) CreateFrames(
        Dictionary<DMIState, Dictionary<Direction, List<FrameInfo>>> frameInfo)
    {
        var animations = new Dictionary<string, Tuple<int, int>>();
        var animationsGroups = frameInfo
            .SelectMany(k => k.Value.Select(v => Tuple.Create(k.Key, v.Key, $"{k.Key.name}_{v.Key}".ToLower())))
            .ToList();

        var frameIdx = 0;
        var jsonBuilder = new StringBuilder();
        for (var i = 0; i < animationsGroups.Count; i++) //)
        {
            var ag = animationsGroups[i];
            var frames = frameInfo[ag.Item1][ag.Item2];
            var startFrame = frameIdx;

            for (var fi = 0; fi < frames.Count; fi++)
            {
                var frame = frames[fi];
                jsonBuilder.Append($"\"frame{frameIdx}.png\": {{");
                jsonBuilder.Append(
                    $"\"frame\": {{\"x\": {frame.Bounds.X}, \"y\": {frame.Bounds.Y}, \"w\": {frame.Bounds.Width}, \"h\": {frame.Bounds.Height}}},");
                jsonBuilder.Append("\"rotated\": false,");
                jsonBuilder.Append("\"trimmed\": false,");
                jsonBuilder.Append(
                    $"\"spriteSourceSize\": {{\"x\": 0, \"y\": 0, \"w\": {frame.Bounds.Width}, \"h\": {frame.Bounds.Height}}},");
                jsonBuilder.Append($"\"sourceSize\": {{\"w\": {frame.Bounds.Width}, \"h\": {frame.Bounds.Height}}},");
                jsonBuilder.Append($"\"duration\": {(int)frame.Delay}");
                jsonBuilder.Append("}");
                frameIdx++;

                if (fi < frames.Count - 1)
                    jsonBuilder.Append(",");
            }

            // Add a comma if it's not the last frame
            if (i < animationsGroups.Count - 1)
                jsonBuilder.Append(",");

            var endFrame = frameIdx - 1;

            animations[ag.Item3] = Tuple.Create(startFrame, endFrame);
        }

        return (jsonBuilder.ToString(), animations);
    }

    private static string GenerateAsepriteJson(Dictionary<DMIState, Dictionary<Direction, List<FrameInfo>>> frameInfo,
        string srcPngName, int imageWidth, int imageHeight, out string[] generatedAnimations)
    {
        //ASESPRITE exports json files and orders the frames into a dictionary.... which is an unordered structure in JSON...
        //so we can't easily generate the JSON while maintaining the order using a JSON serialization library, so we are forced to use
        //string templating..................

        // Create a StringBuilder to construct the JSON document
        var jsonBuilder = new StringBuilder();

        // Start building the JSON
        jsonBuilder.Append("{");

        // Add the "frames" section
        jsonBuilder.Append("\"frames\": {");

        var (frames, animations) = CreateFrames(frameInfo);
        generatedAnimations = animations.Select(x => x.Key).ToArray();
        jsonBuilder.Append(frames);

        // Close the "frames" section
        jsonBuilder.Append("},");

        var animTags = animations.Select(x =>
            $"{{\"name\": \"{x.Key}\",\"from\": {x.Value.Item1},\"to\": {x.Value.Item2},\"direction\": \"forward\"}}");
        var animTagsList = "[" + string.Join(",", animTags) + "]";

        // Add the "meta" section
        jsonBuilder.Append(
            $"\"meta\": {{\"app\": \"http://www.aseprite.org/\",\"version\": \"1.3.9\",\"image\": \"{srcPngName}\",\"format\": \"RGBA8888\",\"size\": {{\"w\": {imageWidth},\"h\": {imageHeight}}},\"scale\": \"1\",\"frameTags\": {animTagsList},\"layers\": [{{\"name\": \"Layer 1\",\"opacity\": 255,\"blendMode\": \"normal\"}}],\"slices\": []}}");

        // Close the JSON document
        jsonBuilder.Append("}");

        // Return the JSON as a string
        return jsonBuilder.ToString();
    }

    public static void Compile(AudioConverter audioConverter, string sourceDirectory, string outputDirectory, string resourceDefinition)
    {

        if (!Directory.Exists(sourceDirectory))
            throw new Exception("Source directory does not exist!");

        if (!Directory.Exists(outputDirectory))
            throw new Exception("Destination directory does not exist!");

        Process(audioConverter, sourceDirectory, outputDirectory, resourceDefinition);
    }
}