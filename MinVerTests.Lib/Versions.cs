using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CliWrap;
using MinVer.Lib;
using MinVerTests.Infra;
using MinVerTests.Lib.Infra;
using Xunit;
using static MinVerTests.Infra.FileSystem;
using static MinVerTests.Infra.Git;

namespace MinVerTests.Lib
{
    public static class Versions
    {
        [Fact]
        public static async Task RepoWithHistory()
        {
            // arrange
            var historicalCommands =
@"git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.0.0-alpha.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.0.0
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.1.0-beta.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.1.0
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.0.0-alpha.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.0.0-rc.1
git tag 1.0.0
git checkout -b foo
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.0.1-alpha.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.0.1
git commit --allow-empty -m '.'
git checkout main
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.1.0-alpha.1
git commit --allow-empty -m '.'
git merge foo --no-edit
git commit --allow-empty -m '.'
git tag 1.1.0-beta.2
git tag 1.1.0-beta.10
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.1.0-rc.1
git tag 1.1.0 -a -m '.'";

            var path = MethodBase.GetCurrentMethod().GetTestDirectory();

            await EnsureEmptyRepositoryAndCommit(path);

            foreach (var command in historicalCommands.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var nameAndArgs = command.Split(" ", 2);
                _ = await Cli.Wrap(nameAndArgs[0]).WithArguments(nameAndArgs[1]).WithWorkingDirectory(path).ExecuteAsync();
                await Task.Delay(200);
            }

            // act
            var versionCounts = new Dictionary<string, int>();
            foreach (var sha in await GetCommitShas(path))
            {
                await Checkout(path, sha);

                var version = Versioner.GetVersion(path, default, default, default, default, default, default);
                var versionString = version.ToString();
                var tagName = $"v/{versionString}";

                _ = versionCounts.TryGetValue(versionString, out var oldVersionCount);
                var versionCount = oldVersionCount + 1;
                versionCounts[versionString] = versionCount;

                tagName = versionCount > 1 ? $"v({versionCount})/{versionString}" : tagName;

                await Tag(path, tagName, sha);
            }

            await Checkout(path, "main");

            // assert
            await AssertFile.Contains("../../../versions.txt", await GetGraph(path));
        }

        [Fact]
        public static async Task EmptyRepo()
        {
            // arrange
            var path = MethodBase.GetCurrentMethod().GetTestDirectory();
            await EnsureEmptyRepository(path);

            // act
            var version = Versioner.GetVersion(path, default, default, default, default, default, default);

            // assert
            Assert.Equal("0.0.0-alpha.0", version.ToString());
        }

        [Fact]
        public static void NoRepo()
        {
            // arrange
            var path = MethodBase.GetCurrentMethod().GetTestDirectory();
            EnsureEmptyDirectory(path);

            // act
            var version = Versioner.GetVersion(path, default, default, default, default, default, default);

            // assert
            Assert.Equal("0.0.0-alpha.0", version.ToString());
        }
    }
}
