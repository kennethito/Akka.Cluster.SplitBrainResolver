using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Medallion.Shell;
using Xunit.Abstractions;
using System.IO;

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
            await RestoreMultiNodeTestRunner(mntrRoot);

            string mntrDir = 
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    mntrRoot, 
                    "Akka.MultiNodeTestRunner.1.3.2-beta439", "lib", "net452");

            string mntrPath = Path.Combine(mntrDir, "Akka.MultiNodeTestRunner.exe");

            string testAssemblyPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Akka.Cluster.SplitBrainResolver.Tests.exe");

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

            _output.WriteLine(await testCommand.StandardOutput.ReadToEndAsync());
            _output.WriteLine(await testCommand.StandardError.ReadToEndAsync());
            await testCommand.Task;

            Assert.True(testCommand.Result.Success, "MultiNodeTests have failed");
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
