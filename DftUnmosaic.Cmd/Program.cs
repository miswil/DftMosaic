using DftMosaic.Core.Files;

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
                var dir = Path.GetDirectoryName(file);
                if (dir is null)
                {
                    Console.WriteLine($"Invalid file path: {file}");
                    continue;
                }
                var outputFile =
                Path.Combine(
                    dir,
                    $"{Path.GetFileNameWithoutExtension(file)}_inv{extension}");
                try
                {
                    var imageFileService = new ImageFileService();
                    using var image = new ImageFileService().Load(file);
                    using var unmosaiced = image.Unmosaic();
                    imageFileService.Save(unmosaiced, outputFile);
                }
                catch (ImageFormatNotSupportedException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine("The supported formats are below.");
                    Console.Error.WriteLine(String.Join(Environment.NewLine, ex.SupportedFormats
                            .Select(f => $"{f.Description}:     {string.Join(", ", f.Extensions)}")));
                }
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