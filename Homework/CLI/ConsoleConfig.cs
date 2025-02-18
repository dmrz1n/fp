﻿using System.Drawing;
using System.Drawing.Imaging;
using TagsCloudContainer.ClientsInterfaces;
using TagsCloudContainer.CloudLayouters;
using TagsCloudContainer.PaintConfigs;
using TagsCloudContainer.TextParsers;

namespace CLI
{
    public class ConsoleConfig : IUserConfig
    {
        public string InputFilePath { get; set; }
        public string InputFileFormat { get; set; }
        public string OutputFilePath { get; set; }
        public string OutputFileName { get; set; }
        public string TagsFontName { get; set; }
        public int TagsFontSize { get; set; }
        public string[] Tags { get; set; }

        public ImageFormat ImageFormat { get; set; }
        public HandlersStorage HandlersStorage { get; set; }
        public Size ImageSize { get; set; }
        public Point ImageCenter { get; set; }
        public ISpiral Spiral { get; set; }
        public IColorScheme ColorScheme { get; set; }
        public ISourceReader SourceReader { get; set; }
        public ITextParser TextParser { get; set; }
    }
}
