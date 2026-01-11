using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security; // 引用 SslClientAuthenticationOptions
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LiteMonitor.src.Core;
using Debug = System.Diagnostics.Debug;

namespace LiteMonitor.src.SystemServices
{
    public class DriverInstaller
    {
        private readonly Settings _cfg;
        private readonly Computer _computer;
        private readonly Action _onReloadRequired; // 回调：通知主程序重载

        // 建议把最快的 Gitee/国内源放在第一个
        private readonly string[] _driverUrls = new[]
        {
            "https://gitee.com/Diorser/LiteMonitor/raw/master/resources/assets/PawnIO_setup.exe",
            "https://litemonitor.cn/update/PawnIO_setup.exe", 
            "https://github.com/Diorser/LiteMonitor/raw/master/resources/assets/PawnIO_setup.exe" 
        };

        private const string ManualDownloadPage = "https://gitee.com/Diorser/LiteMonitor/raw/master/resources/assets/PawnIO_setup.exe";

        public DriverInstaller(Settings cfg, Computer computer, Action onReloadRequired)
        {
            _cfg = cfg;
            _computer = computer;
            _onReloadRequired = onReloadRequired;
        }

        public async Task SmartCheckDriver()
        {
            if (!_cfg.IsAnyEnabled("CPU")) return;

            bool isDriverInstalled = IsPawnIOInstalled();
            var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            bool isCpuValid = cpu != null && cpu.Sensors.Length > 0;

            if (!isDriverInstalled || !isCpuValid)
            {
                if (!isDriverInstalled)
                {
                    Debug.WriteLine("[Driver] Driver missing. Attempting silent install...");
                    bool installed = await SilentDownloadAndInstall();
                    
                    if (installed)
                    {
                        Debug.WriteLine("[Driver] Installed. Reloading...");
                        _onReloadRequired?.Invoke();
                    }
                }
            }
        }

        private bool IsPawnIOInstalled()
        {
            try
            {
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";
                using var k1 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(keyPath);
                if (k1 != null) return true;
                using var k2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(keyPath);
                if (k2 != null) return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 极速切换的下载逻辑 (优化版：随机文件名 + 强制清理 + SSL修复)
        /// </summary>
        private async Task<bool> SilentDownloadAndInstall()
        {
            // 使用 GUID 生成随机文件名，彻底解决“文件被占用”或“覆盖失败”的问题
            string tempFileName = $"PawnIO_{Guid.NewGuid()}.exe";
            string tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            
            bool downloadSuccess = false;

            try 
            {
                // 使用 SocketsHttpHandler 来控制 SSL 选项，忽略证书错误
                using (var handler = new SocketsHttpHandler
                {
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                    }
                })
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "LiteMonitor-AutoUpdater");

                    foreach (var url in _driverUrls)
                    {
                        if (string.IsNullOrWhiteSpace(url)) continue;

                        try
                        {
                            Debug.WriteLine($"[Driver] Trying: {url}");

                            // 设置 15秒 超时
                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                            {
                                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                                
                                if (response.IsSuccessStatusCode)
                                {
                                    var data = await response.Content.ReadAsByteArrayAsync(cts.Token);
                                    
                                    // 写入文件
                                    await File.WriteAllBytesAsync(tempPath, data, cts.Token);

                                    if (new FileInfo(tempPath).Length > 1024)
                                    {
                                        downloadSuccess = true;
                                        Debug.WriteLine("[Driver] Download success.");
                                        break; 
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Driver] Error: {url} -> {ex.Message}");
                        }
                    }
                }

                if (!downloadSuccess)
                {
                    ShowManualFailDialog("下载超时或连接失败，请检查网络。");
                    return false;
                }

                // ================================================================
                // 安装逻辑
                // ================================================================
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "-install -silent",
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        // 等待安装程序结束
                        await proc.WaitForExitAsync();
                        return proc.ExitCode == 0;
                    }
                }
                catch { } // UAC 取消
                
                ShowManualFailDialog("自动安装被取消或拦截。");
                return false;
            }
            finally
            {
                // 无论成功还是失败，最后都尝试删除这个临时文件
                try 
                { 
                    if (File.Exists(tempPath)) File.Delete(tempPath); 
                } 
                catch { /* 忽略删除失败 */ }
            }
        }

        private void ShowManualFailDialog(string reason)
        {
            // 确保在 UI 线程弹窗
            if (Application.OpenForms.Count > 0)
            {
                Application.OpenForms[0]?.Invoke(new Action(() => 
                {
                    DoShowDialog(reason);
                }));
            }
            else
            {
                DoShowDialog(reason);
            }
        }

        private void DoShowDialog(string reason)
        {
             var result = MessageBox.Show(
                $"PawnIO驱动缺失！\n\nLiteMonitor 无法自动配置 CPU 所需的PawnIO驱动\n将无法读取部分CPU数据！\n原因：{reason}\n\n点击“确定”手动下载安装。",
                "LiteMonitor",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.OK)
            {
                try { Process.Start(new ProcessStartInfo(ManualDownloadPage) { UseShellExecute = true }); } catch { }
            }
        }
    }
}