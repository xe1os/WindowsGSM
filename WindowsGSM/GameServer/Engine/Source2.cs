﻿using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text;

/// <summary>
/// 
/// Notes:
/// cs2.exe works almost perfect on WindowsGSM.
/// The one that cause it not perfect is RedirectStandardInput=true will cause an Engine Error - CTextConsoleWin32::GetLine: !GetNumberOfConsoleInputEvents
/// Therefore, SendKeys Input Method is used.
/// 
/// RedirectStandardInput:  NO WORKING
/// RedirectStandardOutput: YES (Used)
/// RedirectStandardError:  YES (Used)
/// SendKeys Input Method:  YES (Used)
/// 
/// </summary>

namespace WindowsGSM.GameServer.Engine
{
    public class Source2
    {
        public Functions.ServerConfig serverData;

        public string Error;
        public string Notice;

        public string StartPath = "game\\bin\\win64\\cs2.exe";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = new Query.A2S();

        public virtual string Port { get { return "27015"; } }
        public virtual string QueryPort { get { return "27015"; } }
        public virtual string GOTVPort { get { return "27020"; } }
        public virtual string Defaultmap { get { return string.Empty; } }
        public virtual string Maxplayers { get { return "10"; } }
        public virtual string Additional { get { return "-nocrashdialog +tv_port {{tv_port}} +clientport {{clientport}}"; } }

        public virtual string Game { get { return string.Empty; } }
        public virtual string AppId { get { return string.Empty; } }

        public Source2(Functions.ServerConfig serverData) => this.serverData = serverData;

        public async Task<Process> Start()
        {
            string Source2Path = Functions.ServerPath.GetServersServerFiles(serverData.ServerID, StartPath);
            if (!File.Exists(Source2Path))
            {
                Error = $"{StartPath} not found ({Source2Path})";
                return null;
            }

            string configPath = Functions.ServerPath.GetServersServerFiles(serverData.ServerID, Game, "cfg/server.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"server.cfg not found ({configPath})";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"-console");
            sb.Append(string.IsNullOrWhiteSpace(Game) ? string.Empty : $" -game {Game}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerIP) ? string.Empty : $" -ip {serverData.ServerIP}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerPort) ? string.Empty : $" -port {serverData.ServerPort}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerGOTVPort) ? string.Empty : $" +tv_port {serverData.ServerGOTVPort}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerMaxPlayer) ? string.Empty : $" -maxplayers{(AppId == "740" ? "_override" : "")} {serverData.ServerMaxPlayer}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerGSLT) ? string.Empty : $" +sv_setsteamaccount {serverData.ServerGSLT}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerParam) ? string.Empty : $" {serverData.ServerParam}");
            sb.Append(string.IsNullOrWhiteSpace(serverData.ServerMap) ? string.Empty : $" +map {serverData.ServerMap}");
            string param = sb.ToString();

            Process p;
            if (!AllowsEmbedConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        FileName = Source2Path,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                        UseShellExecute = false,
                    },
                    EnableRaisingEvents = true
                };
                p.Start();
            }
            else
            {
                p = new Process
                {
                    StartInfo =
                    {
                        FileName = Source2Path,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                        UseShellExecute = false,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };
                var serverConsole = new Functions.ServerConsole(serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "quit");
            });
        }

        public async void CreateServerCFG()
        {
            //Download server.cfg
            string configPath = Functions.ServerPath.GetServersServerFiles(serverData.ServerID, Game, "cfg/server.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, serverData.ServerGame))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", serverData.ServerName);
                configText = configText.Replace("{{rcon_password}}", serverData.GetRCONPassword());
                File.WriteAllText(configPath, configText);
            }

            //Edit WindowsGSM.cfg
            string configFile = Functions.ServerPath.GetServersConfigs(serverData.ServerID, "WindowsGSM.cfg");
            if (File.Exists(configFile))
            {
                string configText = File.ReadAllText(configFile);
                configText = configText.Replace("{{clientport}}", (int.Parse(serverData.ServerPort) - 10).ToString());
                File.WriteAllText(configFile, configText);
            }

            {
                string configText = File.ReadAllText(configFile);
                configText = configText.Replace("{{tv_port}}", (int.Parse(serverData.ServerGOTVPort) - 10).ToString());
                File.WriteAllText(configFile, configText);
            }
            
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(serverData.ServerID, string.Empty, AppId, true);
            Error = steamCMD.Error;

            return p;
        }

        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom);
            Error = error;
            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            Error = $"Invalid Path! Fail to find {StartPath}";
            return File.Exists(Path.Combine(path, StartPath));
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
