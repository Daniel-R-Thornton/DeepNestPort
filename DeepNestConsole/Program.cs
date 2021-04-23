using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using DeepNestLib;

namespace DeepNestConsole
{
    public class NestingOptions
    {
        [Option('i', "input-dir", Required = true, HelpText = "Dxf/Svg input directory")]
        public string InputDirectory { get; set; }
        [Option('o', "output-dir", Required = true, HelpText = "Dxf/Svg output directory")]
        public string OutputDirectory { get; set; }
        [Option('e', "output-extension", Required = false, Default = "svg", HelpText = "Output extension [dxf/svg]")]
        public string OutputExtension { get; set; }
        [Option('w', "sheet-width", Required = false, Default = 2500, HelpText = "Sheet width.")]
        public int SheetWidth { get; set; }
        [Option('h', "sheet-height", Required = false, Default = 1250, HelpText = "Sheet height.")]
        public int SheetHeight { get; set; }
        [Option("sheet-count", Required = false, Default = 200, HelpText = "Number of available sheets.")]
        public int SheetsCount { get; set; }
        [Option("iteration-count", Required = false, Default = 10, HelpText = "Max number of iterations.")]
        public int MaxIterations { get; set; }
        //[Option('a', "approximated", Required = false, HelpText = "Use a combination of default nesting configurations to get fast approximated results.")]
        //public bool Approximated { get; set; }
        [Option('v', "verbose", Required = false, Default = true, HelpText = "Prints detailed progress to standard output.")]
        public bool Verbose { get; set; }
    }
    public class Program
    {
        private static readonly List<string> _supportedExtensions = new() { "svg", "dxf" };
        private static bool _verbose = false;
        private static readonly NestingContext context = new();
        private static StreamWriter logStream;
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<NestingOptions>(args)
                .WithParsed(o =>
                {
                    var timestamp = DateTime.Now.ToString("MMddHHmm");
                    var processTimer = Stopwatch.StartNew();
                    if (!Directory.Exists(o.InputDirectory))
                        throw new ArgumentException("ERROR: Input directory was not found.");
                    if (!_supportedExtensions.Contains(o.OutputExtension))
                        throw new ArgumentException($"ERROR: Output extension is not supported, supported extensions are [{string.Join('|', _supportedExtensions)}].");


                    _verbose = o.Verbose;

                    PrintProgress($"Input Directory: {o.InputDirectory}");
                    PrintProgress($"Output Directory: {o.OutputDirectory}");

                    if (!Directory.Exists(o.OutputDirectory))
                    {
                        Directory.CreateDirectory(o.OutputDirectory);
                        PrintProgress("Output directory was not found, created a new directory ...", MessageType.Warning, true);
                    }

                    logStream = new StreamWriter(File.Create(Path.Combine(o.OutputDirectory, $"nesting_output_{timestamp}.log")));

                    var totalPartsCount = LoadInputs(o.InputDirectory);

                    for (int i = 0; i < o.SheetsCount; i++)
                    {
                        context.AddSheet(o.SheetWidth, o.SheetHeight, i + 1);
                    }

                    PrintProgress($"Added {o.SheetsCount} sheets [{o.SheetWidth}x{o.SheetHeight}] successfully ...");

                    context.StartNest();
                    PrintProgress("Start nesting ...", MessageType.Info, true);
                    PrintProgress("================================================================================");
                    long elapsedMilliseconds = 0;
                    do
                    {
                        var sw = Stopwatch.StartNew();
                        context.NestIterate();
                        var lastNest = context.Nest.nests.Last();
                        var placement = lastNest.placements.First();
                        sw.Stop();
                        elapsedMilliseconds += sw.ElapsedMilliseconds;
                        PrintProgress($"Iteration: {context.Iterations}");
                        PrintProgress($"Fitness: {context.Current.fitness}");
                        PrintProgress($"Parts Placed: {context.PlacedPartsCount}/{totalPartsCount}");
                        PrintProgress($"Sheets Used: {context.UsedSheetsCount}/{o.SheetsCount}");
                        PrintProgress($"Material Utilization: {Math.Round(context.MaterialUtilization * 100.0f, 2)}%");
                        PrintProgress($"Time Elapsed: {sw.ElapsedMilliseconds / 1000} Seconds");
                        PrintProgress($"Total Elapsed Time: {Math.Round(elapsedMilliseconds / 1000.0f, 2)} Seconds");
                        PrintProgress("====================");

                    } while (context.Iterations < o.MaxIterations && !double.IsNaN(context.Current.fitness.GetValueOrDefault()));
                    if (double.IsNaN(context.Current.fitness.GetValueOrDefault()))
                        PrintProgress("Abort nesting as it was unable to converge (Fitness is NaN) ...", MessageType.Warning, true);

                    PrintProgress("Finished Nesting", MessageType.Info, true);
                    switch (o.OutputExtension)
                    {
                        case "dxf":
                            context.ExportDxf(Path.Combine(o.OutputDirectory, $"nesting_output_{timestamp}.dxf"));
                            break;
                        case "svg":
                            context.ExportSvg(Path.Combine(o.OutputDirectory, $"nesting_output_{timestamp}.svg"));
                            break;
                    }

                    processTimer.Stop();
                    PrintProgress("================================================================================");
                    PrintProgress("Output file exported successfully.", MessageType.Info, true);
                    PrintProgress($"Total Elapsed Time: {Math.Round(processTimer.ElapsedMilliseconds / 1000.0f, 2)} Seconds");

                    logStream.Close();
                    logStream.Dispose();
                    logStream = null;
                });
        }

        private static int LoadInputs(string inputDir)
        {
            var inputSvgFiles = Directory.GetFiles(inputDir, "*.svg");
            var inputDxFFiles = Directory.GetFiles(inputDir, "*.dxf");

            if (inputDxFFiles.Any())
                PrintProgress($"Found {inputDxFFiles.Length} DXF files ...");
            if (inputSvgFiles.Any())
                PrintProgress($"Found {inputSvgFiles.Length} SVG files ...");

            var sw = Stopwatch.StartNew();
            var partsCount = AddParts(inputDxFFiles.Concat(inputSvgFiles).ToList(), context);
            sw.Stop();
            PrintProgress($"Added {partsCount}/{inputSvgFiles.Length + inputDxFFiles.Length} parts successfully in {Math.Round(sw.ElapsedMilliseconds / 1000.0f, 2)} seconds ...");

            return partsCount;
        }

        private static int AddParts(List<string> filePaths, NestingContext context)
        {
            int count = 0;

            foreach (var filePath in filePaths)
            {
                var ext = Path.GetExtension(filePath).ToLower();
                RawDetail r = null;
                switch (ext)
                {
                    case ".dxf":
                        r = DxfParser.loadDxf(filePath);
                        break;
                    case ".svg":
                        r = SvgParser.LoadSvg(filePath);
                        break;
                }

                if (r != null)
                {
                    context.ImportFromRawDetail(r, ++count);
                }
            }

            return count;
        }

        public enum MessageType
        {
            Info,
            Warning,
            Error
        }
        private static void PrintProgress(string progress, MessageType type = MessageType.Info, bool enforcePrinting = false)
        {
            var message = $"{type}: {progress}";
            if (_verbose || enforcePrinting)
            {
                Console.WriteLine(message);
            }
            Log(message);
        }

        private static void Log(string progress)
        {
            if (logStream != null)
            {
                logStream.WriteLine(progress);
            }
        }
    }
}
