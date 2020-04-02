﻿using System;
using System.IO;
using Xunit;

namespace ScmBackup.Tests.Integration.Scm
{
    public class CommandLineScmTests
    {
        [Fact]
        public void ReallyExecutes()
        {
            var sut = new FakeCommandLineScm();
            var result = sut.IsOnThisComputer();

            Assert.True(result);
        }

        [Fact]
        public void ThrowsWhenPathFromConfigDoesntExist()
        {
            var sut = new FakeCommandLineScm();

            var config = new Config();
            config.Scms.Add(new ConfigScm { Name = sut.ShortName, Path = sut.FakeCommandNameNotExisting });

            sut.ContextAccess = FakeContext.BuildFakeContextWithConfig(config);

            Assert.Throws<FileNotFoundException>(() => sut.IsOnThisComputer());
        }

        [Fact]
        public void ReallyExecutesWithPathFromConfig()
        {
            var sut = new FakeCommandLineScm();

            var config = new Config();
            config.Scms.Add(new ConfigScm { Name = sut.ShortName, Path = sut.FakeCommandName });

            sut.ContextAccess = FakeContext.BuildFakeContextWithConfig(config);

            var result = sut.IsOnThisComputer();

            Assert.True(result);
        }

        [Fact]
        public void ExecuteReturnsOutput()
        {
            var sut = new FakeCommandLineScm();

            var result = sut.ExecuteCommandDirectly();

            Assert.Equal(sut.FakeCommandResult, result);
        }

        [Fact]
        public void ThrowsWhenContextIsNull()
        {
            var sut = new FakeCommandLineScm();
            sut.ContextAccess = null;

            Assert.Throws<InvalidOperationException>(() => sut.IsOnThisComputer());
        }
    }
}
