using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Medallion.Shell;
using Xunit.Abstractions;
using System.IO;
using FluentAssertions;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    public class MultiNodeTestExecutor
    {
        private readonly ITestOutputHelper _output;

        public MultiNodeTestExecutor(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task RunMultiNodeTests()
        {
            string mntrRoot = "mntr";

            string mntrDir = 
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    mntrRoot, 
                    "Akka.MultiNodeTestRunner.1.3.2", "lib", "net452");

            string mntrPath = Path.Combine(mntrDir, "Akka.MultiNodeTestRunner.exe");

            await RestoreMultiNodeTestRunner(mntrRoot);

            string testAssemblyPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Akka.Cluster.SplitBrainResolver.Tests.dll");

            _output.WriteLine("Executing multinode tests");
            _output.WriteLine($"MultiNodeTestRunner path: {mntrPath}");
            _output.WriteLine($"Assembly under test: {testAssemblyPath}");
            _output.WriteLine(string.Empty);

            var testCommand = 
                Command.Run(
                    mntrPath,
                    new[] { testAssemblyPath },
                    options => 
                    {
                        options.DisposeOnExit();
                        options.Timeout(TimeSpan.FromSeconds(90));
                        options.WorkingDirectory(mntrDir);
                    });

            var output = await testCommand.StandardOutput.ReadToEndAsync();
            var error = await testCommand.StandardError.ReadToEndAsync();

            _output.WriteLine(output);
            _output.WriteLine(error);

            await testCommand.Task;

            testCommand.Result.Success.Should().BeTrue("MultiNodeTests have failed");
            error.Should().BeNullOrEmpty(error);

            output
                //DeathPactException is normal
                .Replace("Akka.Actor.DeathPactException", string.Empty)
                //Discovery related exceptions result in process success, so look for them explicitly
                .IndexOf("exception", StringComparison.OrdinalIgnoreCase) 
                .Should().BeNegative($"There should be no exceptions: {output}");
        }

        private async Task RestoreMultiNodeTestRunner(string relativePath)
        {
            Directory.CreateDirectory(relativePath);

            _output.WriteLine("Nuget restoring MNTR");
            var nuget = Command.Run(
                "nuget",
                new[] { "restore", "-PackagesDirectory", relativePath, "packages.mntr.config" },
                options =>
                {
                    options.DisposeOnExit();
                    options.ThrowOnError();
                    options.Timeout(TimeSpan.FromSeconds(60));
                });

            _output.WriteLine(await nuget.StandardOutput.ReadToEndAsync());
            _output.WriteLine(await nuget.StandardError.ReadToEndAsync());
            await nuget.Task;
        }
    }
}
