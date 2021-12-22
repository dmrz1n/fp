﻿using System.IO;
using Autofac;
using CLI;
using ContainerConfigurers;
using FluentAssertions;
using NUnit.Framework;
using TagsCloudContainer;

namespace CloudContainerTests
{
    [TestFixture]
    public class ApplicationShould
    {
        [Test]
        public void Create_Output_File()
        {
            var input = "input";
            var config = new Client(new[] {input}).UserConfig;
            var container = new AutofacConfigurer(config).GetContainer();

            using (var scope = container.BeginLifetimeScope())
            {
                var painter = scope.Resolve<CloudPainter>();
                painter.Draw();
            }

            File.Exists("tagcloud" + ".Png").Should().BeTrue();
            File.Delete("tagcloud" + ".Png");
        }
    }
}
