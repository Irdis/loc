using System.Text;

public class LocArgs
{
    public string[] Extensions { get; set; }
    public bool ReadFromStdin { get; set; }
    public bool WriteStat { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        if (!ParseArgs(args, out var locArgs))
        {
            return;
        }

        var total = 0;
        const int bufLen = 4096;
        var buf = new char[bufLen];
        EnumeratePaths(locArgs, filePath => {
            if (!File.Exists(filePath))
                return;

            if (locArgs.Extensions != null && locArgs.Extensions.All(ext => !filePath.EndsWith("." + ext)))
                return;

            using var sr = new StreamReader(filePath, new FileStreamOptions {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.SequentialScan
            });
            var fileLoc = 0;
            int read;

            while ((read = sr.Read(buf, 0, bufLen)) != 0)
            {
                for (int i = 0; i < read; i++)
                {
                    if (CheckIsBinary(buf[i]))
                    {
                        return;
                    }
                    else if (buf[i] == '\n')
                    {
                        fileLoc++;
                    }
                }
            }

            if (locArgs.WriteStat)
            {
                Console.WriteLine($"{filePath}:{fileLoc}");
            }

            total += fileLoc;
        });
        Console.WriteLine(total);
    }

    private static bool CheckIsBinary(char ch)
    {
        return ch == '\0';
    }

    private static void EnumeratePaths(LocArgs args, Action<string> func)
    {
        if (args.ReadFromStdin)
        {
            using var input = Console.OpenStandardInput();
            using var reader = new StreamReader(input);
            while (true)
            {
                var filePath = reader.ReadLine();

                if (filePath == null)
                    break;

                filePath = filePath.Trim('"');

                func(filePath);
            }
        }
        else
        {
            foreach (var filePath in Directory.EnumerateFiles(Directory.GetCurrentDirectory() , "*.*", SearchOption.AllDirectories))
            {
                func(filePath);
            }
        }
    }

    private static bool ParseArgs(string[] args, out LocArgs res)
    {
        res = new LocArgs();
        var ind = 0;
        while (ind < args.Length)
        {
            var arg = args[ind];
            if (arg == "-e" && ind < args.Length - 1)
            {
                ind++;
                res.Extensions = args[ind].Split(';', StringSplitOptions.None);
                ind++;
            }
            else if (arg == "-std")
            {
                ind++;
                res.ReadFromStdin = true;
            }
            else if (arg == "-s")
            {
                ind++;
                res.WriteStat = true;
            }
            else if (arg == "-?")
            {
                PrintUsage(Console.Out);
                return false;
            }
            else
            {
                PrintInvalidArgs();
                return false;
            }
        }
        return true;
    }

    private static void PrintInvalidArgs()
    {
        var tw = Console.Error;
        tw.WriteLine("Invalid argument list");
        tw.WriteLine();
        PrintUsage(tw);
    }

    private static void PrintUsage(TextWriter tw)
    {
        tw.WriteLine("loc [args]:");
        tw.WriteLine("    -e <ext1;ext2 ...>: extensions(s)");
        tw.WriteLine("    -std:               read paths from STDIN");
        tw.WriteLine("    -s:                 write stat");
        tw.WriteLine("    -?:                 show this message");
    }
}
