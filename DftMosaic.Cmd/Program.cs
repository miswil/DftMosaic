using DftMosaic.Core.Files;
using DftMosaic.Core.Images;
using OpenCvSharp;
using System.Text.RegularExpressions;

namespace DftMosaic.Cmd
{
    class Program
    {
        private enum Mode { Convert, Help, }

        private static List<Rect> mosaicAreas = new List<Rect>();
        private static string? mosaicedFile;
        private static string? outputFile;
        private static MosaicType mosaicType = MosaicType.GrayScale;
        static void Main(string[] args)
        {
            var dir = Directory.GetCurrentDirectory();
            switch (ParseArg(args))
            {
                case Mode.Convert:
                    Convert();
                    break;
                case Mode.Help:
                    Help();
                    break;
            }
   
        }

        private static void Convert()
        {
            if (mosaicedFile is null || !mosaicAreas.Any() || outputFile is null)
            {
                throw new InvalidOperationException("An image file and mosaiced area and output file must be specified.");
            }
            try
            {
                var imageFileService = new ImageFileService();
                using var image = new ImageFileService().Load(mosaicedFile);
                using var mosaiced = image.Mosaic(
                    mosaicAreas,
                    mosaicType);
                imageFileService.Save(mosaiced, outputFile);
            }
            catch (ImageFormatNotSupportedException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine("The supported formats are below.");
                Console.Error.WriteLine(String.Join(Environment.NewLine, ex.SupportedFormats
                        .Select(f => $"{f.Description}:     {string.Join(", ", f.Extensions)}")));
            }
        }

        private static void Help()
        {
            Console.WriteLine("usage: DftMosaic.Cmd.exe -m X,Y,W,H[;X2,Y2,W2,H2...]");
            Console.WriteLine("       [-h | --help] [-o | --output <output image>");
            Console.WriteLine("       [-t | --type <type>]");
            Console.WriteLine("       <input image>");
            Console.WriteLine();
            Console.WriteLine("    X,Y,W,H: Specify the area to be mosaiced. No space is allowed for before and after the comma.");
            Console.WriteLine("      X: An integer value. Top position of the mosaiced area.");
            Console.WriteLine("      Y: An integer value. Left position of the mosaiced area.");
            Console.WriteLine("      W: An integer value. Width of the mosaiced area.");
            Console.WriteLine("      H: An integer value. Height of the mosaiced area.");
            Console.WriteLine("      Multiple areas can be specified by semi colon separation.");
            Console.WriteLine("    <output image>: Specify an file name of output image. Default \"<input image name>_cnv.<imput image extension>\"");
            Console.WriteLine("    -t or --type option: Specify the mosaic type.");
            Console.WriteLine("    <type>: \"gray\": Treat the input image as a gray scale image.");
            // Console.WriteLine("            \"fullcolor\": Treat the input image as a color image. Output file is 32 bit Tiff image.");
            Console.WriteLine("            \"color\": Treat the input image as a color image. Output file is 16 bit image.");
            Console.WriteLine("            \"shortcolor\": Treat the input image as a color image. Output file is 8 bit image.");
            Console.WriteLine("    <input image>: Specify an image file to be mosaiced.");
        }

        private static Mode ParseArg(string[] args)
        {
            // no arg
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid arguments.");
                return Mode.Help;
            }
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-h" or "--help":
                            return Mode.Help;
                        case "-m" or "--mosaic":
                            var mOption = args[++i];
                            var regex = new Regex(@"(?<x>\d+),(?<y>\d+),(?<w>\d+),(?<h>\d+);?");
                            var matches = regex.Matches(mOption);
                            foreach (Match match in matches)
                            {
                                if (!match.Success)
                                {
                                    Console.WriteLine("Invalid format mosaic area.");
                                    return Mode.Help;
                                }
                                mosaicAreas.Add(new(
                                    int.Parse(match.Groups["x"].Value),
                                    int.Parse(match.Groups["y"].Value),
                                    int.Parse(match.Groups["w"].Value),
                                    int.Parse(match.Groups["h"].Value)
                                    ));
                            }
                            break;
                        case "-o" or "--output":
                            var oOption = args[++i];
                            outputFile = oOption;
                            break;
                        case "-t" or "--type":
                            var tOption = args[++i];
                            switch (tOption)
                            {
                                case "gray":
                                    mosaicType = MosaicType.GrayScale;
                                    break;
                                // 64bit イメージはLogLuv high dynamic range encodingで保存されるため、情報が失われ逆変換時に不鮮明な画像となるのでDeprecate
                                //case "fullcolor":
                                //    mosaicType = MosaicType.FullColor;
                                //    break;
                                case "color":
                                    mosaicType = MosaicType.Color;
                                    break;
                                case "shortcolor":
                                    mosaicType = MosaicType.ShortColor;
                                    break;
                                default:
                                    Console.WriteLine("Invalid arguments.");
                                    return Mode.Help;
                            }
                            break;
                        default:
                            mosaicedFile = args[i];
                            break;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Invalid argument.");
                return Mode.Help;
            }
            if (!mosaicAreas.Any() || mosaicedFile is null)
            {
                Console.WriteLine("Invalid argument.");
                return Mode.Help;
            }
            if (outputFile is null)
            {
                var dir = Path.GetDirectoryName(mosaicedFile);
                if (dir is null)
                {
                    Console.WriteLine($"Invalid file path: {mosaicedFile}");
                    return Mode.Help;
                }
                outputFile =
                    Path.Combine(
                        dir,
                        $"{Path.GetFileNameWithoutExtension(mosaicedFile)}_cnv{Path.GetExtension(mosaicedFile)}");
            }
            return Mode.Convert;
        }
    }
}
