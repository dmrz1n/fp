﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using TagsCloudContainer;
using TagsCloudContainer.ClientsInterfaces;
using TagsCloudContainer.CloudLayouters;
using TagsCloudContainer.PaintConfigs;
using TagsCloudContainer.TextParsers;

namespace CLI
{
    public class Client : IClient
    {
        private readonly string[] args;
        public IUserConfig UserConfig { get; }

        public Client(string[] args)
        {
            this.args = args;
            UserConfig = ParseArguments().GetValueOrThrow();
        }

        private Result<ConsoleConfig> ParseArguments()
        {
            var config = new Result<ConsoleConfig>();
            ParserResult<Options> result;
            try
            {
                result = Parser.Default.ParseArguments<Options>(args);
            }
            catch (Exception e)
            {
                return Result.Fail<ConsoleConfig>(e.Message);
            }

            result.WithParsed(options => config = GetConfigResult(options))
                .WithNotParsed(errs =>
                {
                    if (IsNotHelpOrVersionCall(errs.ToList()))
                        config = Result.Fail<ConsoleConfig>("Error's describing is located above options list");
                });

            return config;
        }

        private bool IsNotHelpOrVersionCall(List<Error> errs)
        {
            return !errs.IsHelp() && !errs.IsVersion();
        }

        private Result<ConsoleConfig> GetConfigResult(Options options)
        {
            var config = new ConsoleConfig();
            return config.AsResult()
                .Then(UseOutputPathFrom, options)
                .Then(UseOutputFileNameFrom, options)
                .Then(UseImageSizeFrom, options)
                .Then(BuildImageCenter)
                .Then(UseFontParamsFrom, options)
                .Then(UseColorSchemeFrom, options)
                .Then(UseSpiralFrom, options)
                .Then(UseImageFormatFrom, options)
                .Then(UseInputFileFormatFrom, options)
                .Then(UseSourceReaderFrom, options)
                .Then(UseHandlersFrom, options)
                .Then(BuildTextParser);
        }

        private Result<T> CheckUsedArg<T>(T obj, Func<T, bool> predicate, string errorMessage)
        {
            return predicate(obj)
                ? Result.Ok(obj)
                : Result.Fail<T>(errorMessage);
        }

        private Result<ConsoleConfig> UseOutputPathFrom(ConsoleConfig config,
            Options options)
        {
            config.OutputFilePath = options.OutputFilePath;
            return CheckUsedArg(config, c => Directory.Exists(c.OutputFilePath), "OutputFilePath directory doesn't exist!");
        }

        private Result<ConsoleConfig> UseOutputFileNameFrom(ConsoleConfig config,
            Options options)
        {
            config.OutputFileName = options.OutputFileName;
            return config.AsResult();
        }

        private Result<ConsoleConfig> UseImageSizeFrom(ConsoleConfig config,
            Options options)
        {
            config.ImageSize = new Size(options.Width, options.Height);
            return CheckUsedArg(config, c => c.ImageSize.Width > 0 && c.ImageSize.Height > 0,
                "Image size must be greater than zero!");
        }

        private Result<ConsoleConfig> BuildImageCenter(ConsoleConfig config)
        {
            var imageSize = config.ImageSize;
            config.ImageCenter = new Point(imageSize.Width / 2, imageSize.Height / 2);
            return config.AsResult();
        }

        private Result<ConsoleConfig> UseFontParamsFrom(ConsoleConfig config,
    Options options)
        {
            config.TagsFontName = options.FontName;
            config.TagsFontSize = options.FontSize;
            return CheckUsedArg(config, CheckFontParams, "Font name is unknown!");
        }

        private bool CheckFontParams(ConsoleConfig config)
        {
            using (var font = new Font(config.TagsFontName, config.TagsFontSize))
                return font.Name == config.TagsFontName;
        }

        private Result<ConsoleConfig> UseColorSchemeFrom(ConsoleConfig config,
            Options options)
        {
            config.ColorScheme = GetColorScheme(options);
            return CheckUsedArg(config, c => c.ColorScheme != null, "Unknown color scheme is given!");
        }

        private IColorScheme GetColorScheme(Options options)
        {
            switch (options.Color)
            {
                case 0: return new BlackWhiteScheme();
                case 1: return new CamouflageScheme();
                case 2: return new CyberpunkScheme();
                default: return null;
            }
        }

        private Result<ConsoleConfig> UseSpiralFrom(ConsoleConfig config,
           Options options)
        {
            config.Spiral = GetSpiral(options);
            return CheckUsedArg(config, c => c.Spiral != null, "Unknown color scheme is given!");
        }

        private ISpiral GetSpiral(Options options)
        {
            var spiralName = options.Spiral.ToLower();
            var imageCenter = new Point(options.Width / 2, options.Height / 2);
            switch (spiralName)
            {
                case "log": return new LogarithmSpiral(imageCenter);
                case "sqr": return new SquareSpiral(imageCenter);
                case "rnd": return new RandomSpiral(imageCenter);
                default: return null;
            }
        }

        private Result<ConsoleConfig> UseImageFormatFrom(ConsoleConfig config,
            Options options)
        {
            config.ImageFormat = GetImageFormat(options);
            return CheckUsedArg(config, c => c.ImageFormat != null, "Unknown color scheme is given!");
        }

        private ImageFormat GetImageFormat(Options options)
        {
            var formatName = options.OutputFileFormat.ToLower();
            switch (formatName)
            {
                case "png": return ImageFormat.Png;
                case "bmp": return ImageFormat.Bmp;
                case "jpeg": return ImageFormat.Jpeg;
                default: return null;
            }
        }

        private Result<ConsoleConfig> UseInputFileFormatFrom(ConsoleConfig config,
            Options options)
        {
            config.InputFileFormat = options.InputFileFormat;
            return CheckUsedArg(config, c => c.InputFileFormat == "txt", "Unknown input file format is given!");
        }

        private Result<ConsoleConfig> UseSourceReaderFrom(ConsoleConfig config,
           Options options)
        {
            config.SourceReader = GetSourceReader(options);
            return CheckUsedArg(config, c => c.SourceReader != null,
                "You need to set input file or words list!");
        }

        private ISourceReader GetSourceReader(Options options)
        {
            var inputFile = options.Input;
            var firstTag = options.Tags.FirstOrDefault();
            if (AreBothGiven(inputFile, firstTag) || AreBothEmpty(inputFile, firstTag)) return null;
            if (inputFile != null) return new TxtFileReader(options.Input);
            return new TagsReader(options.Tags);
        }

        private static bool AreBothEmpty(string inputFile, string firstTag)
            => string.IsNullOrEmpty(inputFile) && string.IsNullOrEmpty(firstTag);

        private static bool AreBothGiven(string inputFile, string firstTag)
            => !string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(firstTag);

        private Result<ConsoleConfig> UseHandlersFrom(ConsoleConfig config, Options options)
        {
            var wordExcludingHandler = GetExcludingHandler(options);
            config.HandlersStorage = new HandlersStorage(options.Modifications);
            config.HandlersStorage.AddCustomHandler(wordExcludingHandler);
            return CheckUsedArg(config, CheckConfigHandlers, GetHandlersError(config));
        }

        private bool CheckConfigHandlers(ConsoleConfig config)
        {
            return config.HandlersStorage.GetUnrecognizedHandlers().Count == 0;
        }

        private string GetHandlersError(ConsoleConfig config)
        {
            var message = new StringBuilder("Unknown handlers list:\n");
            var unknowHandlers = config.HandlersStorage.GetUnrecognizedHandlers();
            foreach (var handler in unknowHandlers)
            {
                message.Append(handler);
                message.Append("\n");
            }

            return message.ToString();
        }

        private Func<string, string> GetExcludingHandler(Options options)
        {
            return word =>
            {
                var excluded = options.ExcludedWords.ToHashSet();
                var boringWords = new BoringWords().GetWords();
                if (excluded.Contains(word) || boringWords.Contains(word)) return "";
                return word;
            };
        }

        private Result<ConsoleConfig> BuildTextParser(ConsoleConfig config)
        {
            config.TextParser = new TextParser(
                config.SourceReader,
                config.HandlersStorage.GetUsingHandlers(),
                GetWordGrouper());
            return config.AsResult();
        }

        private static Action<string, Dictionary<string, int>> GetWordGrouper()
        {
            return (word, dict) =>
            {
                if (word == "") return;
                if (!dict.TryGetValue(word, out _)) dict.Add(word, 0);
                dict[word]++;
            };
        }
    }
}
