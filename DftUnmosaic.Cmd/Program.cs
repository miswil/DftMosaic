using DftMosaic.Core.Mosaic.Files;
using OpenCvSharp;

namespace DftUnmosaic.Cmd
{
    class Program
    {
        private enum Mode { Invert, Help, }

        private static List<string> targetFiles = new List<string>();
        private static string? outputExtension;

        static void Main(string[] args)
        {
            switch (ParseArg(args))
            {
                case Mode.Invert:
                    Invert();
                    break;
                case Mode.Help:
                    Help();
                    break;
            }
        }

        private static void Invert()
        {
            foreach (var file in targetFiles)
            {
                var extension = outputExtension ?? Path.GetExtension(file);
                var outputFile =
                Path.Combine(
                    Path.GetDirectoryName(file),
                    $"{Path.GetFileNameWithoutExtension(file)}_inv{extension}");
                var image = new ImageFile(file);
                var unmosaicer = image.ToUnmosaicer();
                unmosaicer.Unmosaic();
                new ImageFile(unmosaicer).Save(outputFile);
                Cv2.ImWrite(outputFile, unmosaicer.OriginalImage);
            }
        }

        private static void Help()
        {
            Console.WriteLine("usage: DftUnmosaic.Cmd.exe <input image1> [<input image2...]");
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
                        case "-e" or "--extension":
                            var eOption = args[++i];
                            outputExtension = $".{eOption.ToLower()}";
                            break;
                        default:
                            targetFiles.Add(args[i]);
                            break;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Invalid arguments.");
                return Mode.Help;
            }
            return Mode.Invert;
        }
    }
}