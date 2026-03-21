using System.Diagnostics;
using System.IO;

namespace NetTool.UI.Services
{
    public class EngineRunner : IDisposable
    {
        private Process? _process;
        private bool _disposed;

        /// <summary>
        /// Fired when a line is received from engine stdout (JSON metrics).
        /// </summary>
        public event Action<string>? OnStdOutReceived;

        /// <summary>
        /// Fired when a line is received from engine stderr (log/status messages).
        /// </summary>
        public event Action<string>? OnStdErrReceived;

        /// <summary>
        /// Fired when the engine process exits.
        /// </summary>
        public event Action<int>? OnExited;

        /// <summary>
        /// Whether the engine is currently running.
        /// </summary>
        public bool IsRunning => _process != null && !_process.HasExited;

        /// <summary>
        /// Start the Go engine with the given command and config file path.
        /// </summary>
        public void Start(string enginePath, string command, string configPath)
        {
            if (IsRunning)
                throw new InvalidOperationException("Engine is already running.");

            if (!File.Exists(enginePath))
                throw new FileNotFoundException($"Engine not found: {enginePath}");

            var psi = new ProcessStartInfo
            {
                FileName = enginePath,
                Arguments = $"{command} \"{configPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
            };

            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    OnStdOutReceived?.Invoke(e.Data);
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    OnStdErrReceived?.Invoke(e.Data);
            };

            _process.Exited += (_, _) =>
            {
                var exitCode = 0;
                try { exitCode = _process?.ExitCode ?? 0; } catch { }
                OnExited?.Invoke(exitCode);
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        /// <summary>
        /// Stop the engine by killing the process.
        /// </summary>
        public void Stop()
        {
            if (_process == null) return;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(3000);
                }
            }
            catch (Exception)
            {
                // Process may have already exited
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
