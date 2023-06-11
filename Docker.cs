using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortableDocker
{
    public delegate void ProgressStatus(string msg);
    public delegate void DockerResults(DockerResult msg);
    public class DockerResult
    {
        public bool Success = false;
        public string Output = null;
    }

    public class Docker
    {
        private enum DockerOperatingMode
        {
            DockerHostServiceHost,
            DockerHostServiceLib,
            DockerLibServiceLib,
            DockerNoneServiceNone
        }

        private static Process dockerdProcess;
        private string currentDeploymentPath = null;
        private DockerOperatingMode operatingMode = DockerOperatingMode.DockerNoneServiceNone;

        public event ProgressStatus OnProgressStatusChange;
        public event DockerResults OnResults;

        #region Private Methods
        private bool Deploy(string filepath = null)
        {
            currentDeploymentPath = filepath != null
                                  ? filepath : Path.Combine(System.IO.Path.GetTempPath(), GenerateRandom(8) + ".devcoons");
            System.IO.Directory.CreateDirectory(currentDeploymentPath);
            ExtractEmbeddedResource(Path.Combine(currentDeploymentPath, "docker.exe"), libResources.docker);
            ExtractEmbeddedResource(Path.Combine(currentDeploymentPath, "dockerd.exe"), libResources.dockerd);
            ExtractEmbeddedResource(Path.Combine(currentDeploymentPath, "docker-proxy.exe"), libResources.docker_proxy);
            Thread.Sleep(500);
            return true;
        }
        private bool StartLibService()
        {
            if (currentDeploymentPath == null)
                return false;
            try
            {
                dockerdProcess = Process.Start(
                        new ProcessStartInfo(Path.Combine(currentDeploymentPath, "dockerd.exe"), " -H 0.0.0.0:5555")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = true,
                            WorkingDirectory = Environment.CurrentDirectory,
                            Verb = "runas",
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                Thread.Sleep(8500);
                return true;
            }
            catch { }
            return false;
        }
        private string GenerateRandom(int length)
        {
            Random res = new Random();
            var str = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var randomstring = "";

            for (var i = 0; i < length; i++)
            {
                var x = res.Next(str.Length);
                randomstring = randomstring + str[x];
            }
            return randomstring;
        }
        private static void ExtractEmbeddedResource(string file, byte[] resource)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
                File.WriteAllBytes(file, resource);
            }
            catch { }
        }
        private DockerOperatingMode IsServiceActive(int checkStep = -1)
        {
            try
            {
                Process p;

                if (checkStep == 0 || checkStep == -1)
                {
                    p = Process.Start(new ProcessStartInfo("docker", "run hello-world")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        WorkingDirectory = Environment.CurrentDirectory
                    });
                    p.WaitForExit();
                    if (p.StandardOutput.ReadToEnd().TrimEnd().ToUpperInvariant().Contains("HELLO FROM DOCKER!"))
                        return DockerOperatingMode.DockerHostServiceHost;
                }
                if (checkStep == 1 || checkStep == -1)
                {
                    p = Process.Start(new ProcessStartInfo("docker", "-H :5555 run hello-world")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        WorkingDirectory = Environment.CurrentDirectory
                    });
                    p.WaitForExit();
                    if (p.StandardOutput.ReadToEnd().TrimEnd().ToUpperInvariant().Contains("HELLO FROM DOCKER!"))
                        return DockerOperatingMode.DockerHostServiceHost;
                }

                if (checkStep == 2 || checkStep == -1 )
                {
                    if (this.currentDeploymentPath == null)
                        return DockerOperatingMode.DockerNoneServiceNone;


                    p = Process.Start(new ProcessStartInfo(Path.Combine(this.currentDeploymentPath, "docker.exe"), "-H :5555 run hello-world")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        WorkingDirectory = Environment.CurrentDirectory
                    });
                    p.WaitForExit();
                    if (p.StandardOutput.ReadToEnd().TrimEnd().ToUpperInvariant().Contains("HELLO FROM DOCKER!"))
                        return DockerOperatingMode.DockerHostServiceHost;
                }
            }
            catch { }
            return DockerOperatingMode.DockerNoneServiceNone;
        }
        #endregion

        #region Public Methods
        public async Task<bool> Start()
        {
            return await Task.Run(() =>
            {
                try
                {
                    OnProgressStatusChange?.Invoke("Checking Docker operating mode..");
                    operatingMode = IsServiceActive(0);
                    if (operatingMode != DockerOperatingMode.DockerNoneServiceNone)
                    {
                        OnProgressStatusChange?.Invoke("Docker engine ready (" + operatingMode + ")");
                        return true;
                    }
                    OnProgressStatusChange?.Invoke("First stage Docker deployment..");
                    Deploy();
                    OnProgressStatusChange?.Invoke("Checking Docker operating mode..");
                    operatingMode = IsServiceActive(1);
                    if (operatingMode != DockerOperatingMode.DockerNoneServiceNone)
                    {
                        OnProgressStatusChange?.Invoke("Docker engine ready (" + operatingMode + ")");
                        return true;
                    }
                    OnProgressStatusChange?.Invoke("Second stage Docker deployment..");
                    StartLibService();
                    OnProgressStatusChange?.Invoke("Checking Docker operating mode..");
                    operatingMode = IsServiceActive(2);
                    if (operatingMode != DockerOperatingMode.DockerNoneServiceNone)
                    {
                        OnProgressStatusChange?.Invoke("Docker engine ready (" + operatingMode + ")");
                        return true;
                    }
                }
                catch { }
                OnProgressStatusChange?.Invoke("Docker engine could not start");
                return false;
            });
        }
        public async Task<bool> Stop()
        {
            return await Task.Run(() => {

                try { 
                    dockerdProcess?.Kill();
                    return true;
                }
                catch {
                    return false;
                }
            });
        }
        public async Task<DockerResult> Execute(string cmd)
        {
            return await Task.Run(() =>
            {
                if (currentDeploymentPath == null)
                {
                    OnResults?.Invoke(new DockerResult() { Success = false, Output = null });
                    return new DockerResult() { Success = false, Output = null };
                }
                var dockerFilename = "";
                var dockerArguments = "";
                try
                {
                    if (operatingMode != DockerOperatingMode.DockerHostServiceHost)
                    {
                        dockerFilename = "docker";
                        dockerArguments = " " + cmd;
                    }
                    else if (operatingMode != DockerOperatingMode.DockerHostServiceLib)
                    {
                        dockerFilename = "docker";
                        dockerArguments = " -H :5555 " + cmd;
                    }
                    else if (operatingMode != DockerOperatingMode.DockerLibServiceLib)
                    {
                        dockerFilename = Path.Combine(currentDeploymentPath, "docker.exe");
                        dockerArguments = " -H :5555 " + cmd;
                    }
                    else
                    {
                        OnResults?.Invoke(new DockerResult() { Success = false, Output = null });
                        return new DockerResult() { Success = false, Output = null };
                    }
                    var p = Process.Start(new ProcessStartInfo(dockerFilename, dockerArguments)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        WorkingDirectory = Environment.CurrentDirectory
                    });
                    p.WaitForExit();

                    OnResults?.Invoke(p.ExitCode != 0 ? new DockerResult() { Success = false, Output = p.StandardError.ReadToEnd().Trim() }
                                           : new DockerResult() { Success = true, Output = p.StandardOutput.ReadToEnd().Trim() });
                    return p.ExitCode != 0 ? new DockerResult() { Success = false, Output = p.StandardError.ReadToEnd().Trim() }
                                           : new DockerResult() { Success = true, Output = p.StandardOutput.ReadToEnd().Trim() };
                }
                catch { }
                OnResults?.Invoke(new DockerResult() { Success = false, Output = null });
                return new DockerResult() { Success = false, Output = null };
            });
        }
        public static void OnExitOrFailure()
        {
            try { dockerdProcess?.Kill(); }
            catch { }
        }
        #endregion
    }
}
