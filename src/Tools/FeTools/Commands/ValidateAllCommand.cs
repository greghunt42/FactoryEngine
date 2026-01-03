using System.Linq;
using System.Text.Json;
using FactoryEngine.Core.Diagnostics;

namespace FeTools.Commands;

public static class ValidateAllCommand
{
    public static int Run(IEnumerable<string> args, NdjsonLogger logger)
    {
        string? configPath;
        bool stopOnFailure;
        try
        {
            (configPath, stopOnFailure) = ParseArgs(args);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            return 1;
        }

        if (string.IsNullOrWhiteSpace(configPath))
        {
            logger.Error("validate-all requires --config <file>.");
            return 1;
        }

        ValidateAllConfig config;
        try
        {
            var fullPath = Path.GetFullPath(configPath);
            if (!File.Exists(fullPath))
            {
                logger.Error($"Options file '{fullPath}' not found.");
                return 1;
            }

            var json = File.ReadAllText(fullPath);
            config = JsonSerializer.Deserialize<ValidateAllConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ValidateAllConfig();
        }
        catch (Exception ex)
        {
            logger.Error("Failed to parse validate-all config.", ex);
            return 1;
        }

        var failed = false;
        stopOnFailure |= config.StopOnFirstFailure;

        failed |= RunCommand(config.ValidateAssets, AssetValidationCommand.Run, "validate-assets", stopOnFailure, logger);
        if (failed && stopOnFailure)
        {
            return 1;
        }

        failed |= RunCommand(config.ValidateData, (a, l) => DataValidationCommand.Run(a.ToArray(), l), "validate-data", stopOnFailure, logger);
        if (failed && stopOnFailure)
        {
            return 1;
        }

        failed |= RunCommand(config.ValidateModules, (a, l) => ModuleValidationCommand.Run(a.ToArray(), l), "validate-modules", stopOnFailure, logger);

        return failed ? 1 : 0;
    }

    private static (string? ConfigPath, bool StopOnFailure) ParseArgs(IEnumerable<string> args)
    {
        var argArray = args.ToArray();
        string? configPath = null;
        var stopOnFailure = false;
        for (var i = 0; i < argArray.Length; i++)
        {
            var arg = argArray[i];
            switch (arg)
            {
                case "--stop-on-failure":
                    stopOnFailure = true;
                    break;
                case "--config":
                    if (i + 1 >= argArray.Length)
                    {
                        throw new ArgumentException("Missing value for --config");
                    }
                    configPath = argArray[++i];
                    break;
                default:
                    configPath ??= arg;
                    break;
            }
        }

        return (configPath, stopOnFailure);
    }

    private static bool RunCommand(
        IReadOnlyList<string>? commandArgs,
        Func<IEnumerable<string>, NdjsonLogger, int> runner,
        string name,
        bool stopOnFailure,
        NdjsonLogger logger)
    {
        if (commandArgs is null)
        {
            return false;
        }

        try
        {
            logger.Info($"Running {name}...");
            var exitCode = runner(commandArgs, logger);
            if (exitCode != 0)
            {
                logger.Warn($"{name} failed with exit code {exitCode}.");
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"{name} threw an exception.", ex);
            return true;
        }

        return false;
    }

    private sealed class ValidateAllConfig
    {
        public List<string>? ValidateAssets { get; set; }
        public List<string>? ValidateData { get; set; }
        public List<string>? ValidateModules { get; set; }
        public bool StopOnFirstFailure { get; set; }
    }
}
