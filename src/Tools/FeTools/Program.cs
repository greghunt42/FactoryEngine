using System.Linq;
using FeTools.Commands;
using FactoryEngine.Core.Diagnostics;

var logger = new NdjsonLogger("FeTools", Console.Out);

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0] switch
{
    "validate-assets" => AssetValidationCommand.Run(args.Skip(1), logger),
    "validate-data" => DataValidationCommand.Run(args.Skip(1).ToArray(), logger),
    "validate-modules" => ModuleValidationCommand.Run(args.Skip(1).ToArray(), logger),
    "hash" => HashCommand.Run(args.Skip(1).ToArray(), logger),
    "validate-all" => ValidateAllCommand.Run(args.Skip(1), logger),
    "help" => ShowHelp(),
    "--help" => ShowHelp(),
    "-h" => ShowHelp(),
    _ => UnknownCommand(args[0])
};

int ShowHelp()
{
    PrintUsage();
    return 0;
}

int UnknownCommand(string command)
{
    logger.Error($"Unknown command '{command}'");
    PrintUsage();
    return 1;
}

void PrintUsage()
{
    Console.WriteLine("fe-tools commands:");
    Console.WriteLine("  validate-assets [paths...]      Validate catalog manifests (files or directories) via AssetService.");
    Console.WriteLine("  validate-data [options] [paths] Validate prefab data using serialization descriptors.");
    Console.WriteLine("  validate-modules [options] <manifest...> Validate module manifests and type references.");
    Console.WriteLine("  validate-all --config <file>    Run assets/data/modules validations using a shared JSON config.");
    Console.WriteLine("  hash [options] <file|dir...>    Compute hashes for files or directories (defaults to SHA-256).");
    Console.WriteLine("  help                            Show this message.");
}
