using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace HomeService.Transversal.Common.Utils;

public static class ComputerStatusUtils
{
    private const double PingTimeoutSeconds = 1.5;
    private const double SshTimeoutSeconds = 15.0;
    private const int WakeOnLanPort = 9;

    private static readonly string SshKeyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "homelab_key");
    private static readonly Regex MacPattern = new Regex(@"^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);
    private static readonly Regex SshUserPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    #region ComputerStatus
    // True si el equipo responde al ping.
    public static async Task<bool> ComputerStatus(string ipAddress)
    {
        try
        {
            if (!IsValidIp(ipAddress))
                return false;

            string[] arguments = OperatingSystem.IsWindows()
                ? ["-n", "1", "-w", ((int)(PingTimeoutSeconds * 1000)).ToString(CultureInfo.InvariantCulture), ipAddress]
                : ["-c", "1", "-W", "1", ipAddress];

            (int exitCode, string standardOutput, _) = await Run("ping", arguments, PingTimeoutSeconds + 2);

            if (exitCode != 0)
                return false;

            // El ping de Windows devuelve 0 tambien con "Host de destino inaccesible";
            // solo una respuesta real trae TTL.
            if (OperatingSystem.IsWindows())
                return standardOutput.Contains("ttl=", StringComparison.OrdinalIgnoreCase);

            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region SendWakeOnLanPacket
    // Envia el magic packet directamente por UDP.
    public static async Task SendWakeOnLanPacket(string macAddress, string broadcastIp)
    {
        if (!MacPattern.IsMatch(macAddress))
            throw new ArgumentException($"MAC address invalida: {macAddress}", nameof(macAddress));

        if (!IsValidIp(broadcastIp))
            throw new ArgumentException($"IP de broadcast invalida: {broadcastIp}", nameof(broadcastIp));

        byte[] macBytes = Convert.FromHexString(macAddress.Replace(":", "").Replace("-", ""));
        byte[] magicPacket = new byte[6 + (macBytes.Length * 16)];

        magicPacket.AsSpan(0, 6).Fill(0xFF);
        for (int repetition = 0; repetition < 16; repetition++)
            macBytes.CopyTo(magicPacket, 6 + (repetition * macBytes.Length));

        using UdpClient udpClient = new UdpClient() { EnableBroadcast = true };
        await udpClient.SendAsync(magicPacket, new IPEndPoint(IPAddress.Parse(broadcastIp), WakeOnLanPort));
    }
    #endregion

    #region ShutdownComputer
    public static async Task<string> ShutdownComputer(string ipAddress, string sshUser)
    {
        bool isOn = await ComputerStatus(ipAddress);
        if (!isOn)
            return "El ordenador ya esta apagado.";

        (int exitCode, string standardError) = await ShutdownViaSsh(sshUser, ipAddress);

        if (exitCode != 0)
            throw new InvalidOperationException($"SSH poweroff fallo (exit {exitCode}): {standardError}");

        return "Apagando el ordenador (CachyOS via SSH).";
    }
    #endregion

    #region ShutdownViaSsh
    private static async Task<(int ExitCode, string StandardError)> ShutdownViaSsh(string sshUser, string sshHost)
    {
        if (!IsValidIp(sshHost))
            return (1, $"Host SSH invalido: {sshHost}");

        if (!SshUserPattern.IsMatch(sshUser))
            return (1, $"Usuario SSH invalido: {sshUser}");

        string[] arguments =
        [
            "-o", "StrictHostKeyChecking=no",
            "-o", "BatchMode=yes",
            "-i", SshKeyPath,
            $"{sshUser}@{sshHost}",
            "sudo systemctl poweroff",
        ];

        try
        {
            (int exitCode, _, string standardError) = await Run("ssh", arguments, SshTimeoutSeconds);
            return (exitCode, standardError);
        }
        catch (Win32Exception)
        {
            return (1, "No se pudo iniciar el proceso SSH: el binario ssh no esta en el PATH.");
        }
        catch (TimeoutException)
        {
            return (1, $"El proceso SSH no respondio en {SshTimeoutSeconds} segundos.");
        }
    }
    #endregion

    #region Run
    // Los argumentos van en ArgumentList, nunca en un string: asi no hay shell de por
    // medio y no existe el riesgo de inyeccion de comandos.
    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> Run(
        string fileName, IEnumerable<string> arguments, double timeoutSeconds)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"No se pudo iniciar el proceso {fileName}.");

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        using CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"El proceso {fileName} no respondio en {timeoutSeconds} segundos.");
        }

        return (process.ExitCode, await standardOutputTask, await standardErrorTask);
    }
    #endregion

    #region IsValidIp
    private static bool IsValidIp(string value) => IPAddress.TryParse(value, out _);
    #endregion
}
