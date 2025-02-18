﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CLI;
using FluentAssertions;
using NUnit.Framework;
using TagsCloudContainer.ClientsInterfaces;
using TagsCloudContainer.CloudLayouters;
using TagsCloudContainer.PaintConfigs;
using TagsCloudContainer.TextParsers;

namespace CloudContainerTests
{
    [TestFixture]
    internal class ConsoleClientShould
    {
        private static readonly string[] commonArgs;
        private static readonly IUserConfig commonConfig;

        static ConsoleClientShould()
        {
            using (File.Create("input.txt")) { }
            commonArgs = new[]
            {
                "--input", "input.txt", "-c", "2",
                "-h", "5000", "-w", "5000", "-i", 
                "jpeg", "-r", "log", "-s", "20"
            };
            commonConfig = new ConsoleConfig
            {
                InputFilePath = null,
                Tags = null,
                OutputFilePath = Directory.GetCurrentDirectory(),
                OutputFileName = "tagcloud",
                ColorScheme = new CyberpunkScheme(),
                ImageSize = new Size(5000, 5000),
                InputFileFormat = "txt",
                ImageCenter = new Point(2500, 2500),
                ImageFormat = ImageFormat.Jpeg,
                TagsFontName = "Arial",
                TagsFontSize = 20,
                Spiral = new LogarithmSpiral(new Point(2500, 2500)),
                SourceReader = new TxtFileReader("input.txt")
            };

        }

        [SetUp]
        public void CreateInput()
        {
            if (File.Exists("input.txt")) return;
            using (File.Create("input.txt")) { }
        }


        [TearDown]
        public void DeleteInput()
        {
            File.Delete("input.txt");
        }

        [TestCase("--output", "F:\\", TestName = "not existed output directory is given")]
        [TestCase("--words", "two words", TestName = "both custom words and input file path are given")]
        [TestCase("-w", "0", TestName = "width is equal zero")]
        [TestCase("-h", "-5", TestName = "height is less than zero")]
        [TestCase("-c", "4", TestName = "unknown color scheme is given")]
        [TestCase("-f", "png", TestName = "text format is not txt")]
        [TestCase("-i", "doc", TestName = "output image format is unknown")]
        [TestCase("-r", "big", TestName = "unknown spiral is given")]
        [TestCase("-s", "-100", TestName = "font size is equal zero")]
        [TestCase("-n", "Aerials", TestName = "unknown font name is given")]
        [TestCase("-m", "stranger handlers", TestName = "unknown handlers are given")]
        public void Throw_Argument_Exception_When(string key, string arg)
        {
            var arrayArgs = new[] {"--input", "input.txt", key, arg};

            FluentActions.Invoking(() => new Client(arrayArgs)).Should()
                .Throw<ArgumentException>();
        }


        [TestCase("--help", TestName = "help key")]
        [TestCase("--version", TestName = "value key")]
        public void Not_Throw_When_Only_Help_Or_Version_Key_Is_Given(string arg)
        {
            var arrayArgs = new[] { arg };

            FluentActions.Invoking(() => new Client(arrayArgs)).Should()
                .NotThrow<ArgumentException>();
        }

        [TestCaseSource(nameof(Args))]
        public void Make_Config_Correctly_When(string[] args, IUserConfig expectedConfig)
        {
            var config = new Client(args).UserConfig;

            config.Should().BeEquivalentTo(expectedConfig, options =>
                options.Excluding(c => c.HandlersStorage)
                    .Excluding(c => c.TextParser));
        }

        private static IEnumerable<TestCaseData> Args()
        {
            yield return new TestCaseData(
                commonArgs,
                commonConfig
            ).SetName("common case is given");
            commonArgs[9] = "png";
            commonConfig.ImageFormat = ImageFormat.Png;
            yield return new TestCaseData(
                commonArgs,
                commonConfig
            ).SetName("image format is changed");
            commonArgs[11] = "sqr";
            commonConfig.Spiral = new SquareSpiral(commonConfig.ImageCenter);
            yield return new TestCaseData(
                commonArgs,
                commonConfig
            ).SetName("spiral is changed");
            commonArgs[13] = "25";
            commonConfig.TagsFontSize = 25;
            yield return new TestCaseData(
                commonArgs,
                commonConfig
            ).SetName("font size is changed");
            commonArgs[5] = "4000";
            commonConfig.ImageSize = new Size(5000, 4000);
            commonConfig.ImageCenter = new Point(5000 / 2, 4000 /2);
            yield return new TestCaseData(
                commonArgs,
                commonConfig
            ).SetName("image size is changed");
        }
    }
}
