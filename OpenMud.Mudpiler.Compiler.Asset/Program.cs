using System.Diagnostics;
using System.Text;
using SixLabors.ImageSharp.Formats.Png;

namespace OpenMud.Mudpiler.Compiler.Asset;

public static class Program
{
    public enum Direction
    {
        South,
        North,
        East,
        West,
        SouthEast,
        SouthWest,
        NorthEast,
        NorthWest
    }

    private static readonly double DEFAULT_SPEED = 5.0;

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

    public static DMIMetaData ParseMetaData(Image image)
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

    public static void ConvertMidiToWav(string midiFilePath, string wavOutputPath)
    {
        try
        {
            // Replace with the actual path to kmcogg.exe
            var fluidsynthpath =
                @"C:\Users\jerem\OneDrive\Desktop\byond_test_projects\compile_tools\fluidsynth\bin\fluidsynth.exe";
            var soundfontpath =
                @"C:\Users\jerem\OneDrive\Desktop\byond_test_projects\compile_tools\soundfont\FluidR3Mono_GM.sf3";

            // Construct the command line arguments
            var arguments = $"\"{soundfontpath}\" -F \"{wavOutputPath}\" \"{midiFilePath}\"";

            // Create a ProcessStartInfo for the command
            var psi = new ProcessStartInfo
            {
                FileName = fluidsynthpath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Create and start the process
            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                process.WaitForExit();

                // Check if the conversion was successful (exit code 0)
                if (process.ExitCode == 0)
                    Console.WriteLine("Conversion completed successfully.");
                else
                    throw new Exception("Conversion failed. Check the tool and input MIDI file.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static Dictionary<string, string> ProcessAudio(string src, string dst)
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
                ConvertMidiToWav(s, outputFullPath);
            }
            else
            {
                File.Copy(s, Path.Join(dst, relativeFullPath), true);
            }

            soundDirectory[relativeFullPath] = relativeFileName;
        }

        return soundDirectory;
    }

    private static void Process(string src, string dst)
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

        var indexName = $"{dst}/resources.ts";

        GenerateAssetIndex(assetDirectory, animationsDirectory, ProcessAudio(src, dst), indexName);

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
import {{ AsepriteResource }} from ""@excalibur-aseprite"";

const GameIcons = {{
{resourceDecl}
}}

const GameIconAnimationsIndex = {{
{animIdx}
}}

const GameSounds = {{
{soundDecl}
}}


const loader = new Loader()
const allResources: any = {{...GameIcons, ...GameSounds}}
for (const res in allResources) {{
  loader.addResource(allResources[res])
}}

export {{ loader, GameIcons, GameSounds, GameIconAnimationsIndex }}

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

    /*
    private static void BuildTres(Image image, DMIMetaData metaData, string destTres, string destPngDir, string projectRoot)
    {
        Dictionary<DMIState, Dictionary<Direction, string[]>> states = new();


        int origin = 0;
        foreach (var state in metaData.states)
        {
            var action_name = state.name;

            //Each element is a list of frames for the respective direction. Starting N, NE, E, SE, S, SW, W, NW
            Dictionary<Direction, string[]> frames = DumpFrames(image, metaData.width, metaData.height, state.directions, state.frames, Path.Join(destPngDir, $"./{action_name}/"), origin);
            origin += frames.Sum(x => x.Value.Length);

            states[state] = frames;
        }

        WriteTres(states, destTres, projectRoot);
    }


    private static void WriteTres(Dictionary<DMIState, Dictionary<Direction, string[]>> states, string destTres, string projectRoot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destTres));
        var frameIdLookup = states.SelectMany(x => x.Value.Values).SelectMany(x => x.ToList()).Select((f, i) => (f, i)).ToDictionary(x => x.f, x => $"frame_{x.i}");

        //One for each frame, plus the step for creating the animation from the frames.
        int loadSteps = states.Sum(x => x.Value.Sum(n => n.Value.Length)) + 1;

        using (var fos = File.CreateText(destTres))
        {
            fos.WriteLine($"[gd_resource type=\"SpriteFrames\" load_steps={loadSteps} format=3]");

            foreach (var (frame, frame_id) in frameIdLookup)
            {
                var relativePath = Path.GetRelativePath(projectRoot, frame).Replace("\\", "/");
                fos.WriteLine($"[ext_resource type=\"Texture2D\" path=\"res://{relativePath}\" id=\"{frame_id}\"]");
            }

            List<string> animations = new();
            foreach (var (state, stateDirFrames) in states)
            {
                foreach (var (direction, frames) in stateDirFrames)
                {
                    var anim = new StringBuilder();

                    var loopStr = state.rewind != 0 ? "true" : "false";

                    anim.AppendLine("{");
                    anim.AppendLine($"\"name\": &\"{state.name}_{direction.ToString().ToLower()}\",");
                    anim.AppendLine($"\"speed\": {DEFAULT_SPEED},");
                    anim.AppendLine($"\"loop\": {loopStr},");
                    anim.AppendLine($"\"frames\": [");

                    for (int frameIdx = 0; frameIdx < frames.Length; frameIdx++)
                    {
                        anim.Append($"{{\"duration\": {state.delays[frameIdx]}, \"texture\": ExtResource(\"{frameIdLookup[frames[frameIdx]]}\")}}");

                        if (frameIdx == frames.Length - 1)
                            anim.AppendLine();
                        else
                            anim.AppendLine(",");
                    }

                    anim.AppendLine("]}");

                    animations.Add(anim.ToString());
                }
            }

            fos.WriteLine("[resource]");
            fos.WriteLine("animations = [");
            fos.WriteLine(String.Join(",\n", animations));
            fos.WriteLine("]");
        }
    }

    private static Dictionary<Direction, string[]> DumpFrames(Image image, int frameWidth, int frameHeight, int directions, int numFrames, string destDir, int originIdx)
    {
        Directory.CreateDirectory(destDir);

        Image GetImage(int idx) => image.Clone(op => {
            int xFrames = image.Width / frameWidth;
            int yFrames = image.Height / frameHeight;

            int xPos = (idx % xFrames) * frameWidth;
            int yPos = (idx / xFrames) * frameHeight;

            op.Crop(new Rectangle(xPos, yPos, frameWidth, frameHeight));
        });

        var dirMap = new Dictionary<Direction, List<string>>();
        for (int i = 0; i < numFrames; i++)
        {
            for (int w = 0; w < directions; w++)
            {
                var dirName = DMIDirectionOrder[w];

                if (!dirMap.ContainsKey(dirName))
                    dirMap.Add(dirName, new List<string>());

                int srcIdx = originIdx + directions * i + w;
                using (var imageFrame = GetImage(srcIdx))
                {
                    var name = Path.Join(destDir, $"./{dirName}_{i}.png");
                    imageFrame.SaveAsPng(name);

                    dirMap[dirName].Add(name);
                }

            }
        }

        return dirMap.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }*/

    public static void Main(string[] args)
    {
        args = new[]
        {
            "C:\\Users\\jerem\\OneDrive\\Desktop\\byond_test_projects\\astepbyond\\compiled\\assetgen\\",
            "C:\\Users\\jerem\\OneDrive\\Desktop\\byond_test_projects\\astepbyond\\buildup\\"
        };

        if (args.Length != 2)
        {
            Console.Error.WriteLine("Error: Usage is Dmi2Tres [dest_godot_project_path] [source_dmi_directory_path]");
            return;
        }

        if (!Directory.Exists(args[1]))
        {
            Console.Error.WriteLine(
                "Error, first argument must be to a directory structure where DMI files are extracted from.");
            return;
        }

        if (!Directory.Exists(args[0]))
        {
            Console.Error.WriteLine("Error, destination directory does not exist.");
            return;
        }

        Process(args[1], args[0]);
    }

    public struct FrameInfo
    {
        public Rectangle Bounds;
        public float Delay;
    }

    public struct DMIState
    {
        public string name;
        public int directions;
        public int frames;
        public int rewind;
        public float[] delays;
    }

    public struct DMIMetaData
    {
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
}