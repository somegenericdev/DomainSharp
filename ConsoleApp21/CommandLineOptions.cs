using CommandLine;

namespace ConsoleApp21;

public class CommandLineOptions
{
    [Option('i', "input", Required = true, HelpText = "The input file, containing a list of words separated by a newline.")]
    public string InputPath { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output file, containing the available domains found.")]
    public string OutputPath { get; set; }
}
