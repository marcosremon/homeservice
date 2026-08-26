import asyncio
import ipaddress
import re
import socket
import sys
from pathlib import Path

_PING_TIMEOUT_SECONDS = 1.5
_SSH_TIMEOUT_SECONDS = 15.0
_WAKE_ON_LAN_PORT = 9
_SSH_KEY_PATH = Path.home() / ".ssh" / "homelab_key"

_MAC_PATTERN = re.compile(r"^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$")

# region computer_status
async def ComputerStatus(ipAddress: str) -> bool:
    """True si el equipo responde al ping."""
    try:
        if not _isValidIp(ipAddress):
            return False

        if sys.platform == "win32":
            command = ["ping", "-n", "1", "-w", str(int(_PING_TIMEOUT_SECONDS * 1000)), ipAddress]
        else:
            command = ["ping", "-c", "1", "-W", str(int(_PING_TIMEOUT_SECONDS) or 1), ipAddress]

        exitCode, stdout, _ = await _run(command, timeout=_PING_TIMEOUT_SECONDS + 2)

        if exitCode != 0:
            return False

        # El ping de Windows devuelve 0 tambien con "Host de destino inaccesible";
        # solo una respuesta real trae TTL.
        if sys.platform == "win32":
            return "ttl=" in stdout.lower()

        return True
    except Exception:
        return False
# endregion

# region send_wake_on_lan_packet
async def SendWakeOnLanPacket(macAddress: str, broadcastIp: str) -> None:
    """Envia el magic packet directamente por UDP."""
    if not _MAC_PATTERN.match(macAddress):
        raise ValueError(f"MAC address invalida: {macAddress}")

    if not _isValidIp(broadcastIp):
        raise ValueError(f"IP de broadcast invalida: {broadcastIp}")

    macBytes = bytes.fromhex(macAddress.replace(":", "").replace("-", ""))
    magicPacket = b"\xff" * 6 + macBytes * 16

    def _send() -> None:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udpSocket:
            udpSocket.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
            udpSocket.sendto(magicPacket, (broadcastIp, _WAKE_ON_LAN_PORT))

    # sendto es bloqueante: fuera del event loop para no frenar otras peticiones.
    await asyncio.to_thread(_send)
# endregion

# region shutdown_computer
async def ShutdownComputer(ipAddress: str, sshUser: str) -> str:
    isOn = await ComputerStatus(ipAddress)
    if not isOn:
        return "El ordenador ya esta apagado."

    exitCode, stderr = await _shutdownViaSsh(sshUser, ipAddress)

    if exitCode != 0:
        raise RuntimeError(f"SSH poweroff fallo (exit {exitCode}): {stderr}")

    return "Apagando el ordenador (CachyOS via SSH)."
# endregion

# region _shutdown_via_ssh
async def _shutdownViaSsh(sshUser: str, sshHost: str) -> tuple[int, str]:
    if not _isValidIp(sshHost):
        return 1, f"Host SSH invalido: {sshHost}"

    if not sshUser.isidentifier():
        return 1, f"Usuario SSH invalido: {sshUser}"

    command = [
        "ssh",
        "-o", "StrictHostKeyChecking=no",
        "-o", "BatchMode=yes",
        "-i", str(_SSH_KEY_PATH),
        f"{sshUser}@{sshHost}",
        "sudo systemctl poweroff",
    ]

    try:
        exitCode, _, stderr = await _run(command, timeout=_SSH_TIMEOUT_SECONDS)
        return exitCode, stderr
    except FileNotFoundError:
        return 1, "No se pudo iniciar el proceso SSH: el binario ssh no esta en el PATH."
    except asyncio.TimeoutError:
        return 1, f"El proceso SSH no respondio en {_SSH_TIMEOUT_SECONDS} segundos."
# endregion

# region _run
async def _run(command: list[str], timeout: float) -> tuple[int, str, str]:
    """
    Los argumentos van en lista, nunca en un string: asi no hay shell de por
    medio y no existe el riesgo de inyeccion de comandos.
    """
    process = await asyncio.create_subprocess_exec(
        *command,
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE,
    )

    try:
        stdout, stderr = await asyncio.wait_for(process.communicate(), timeout=timeout)
    except asyncio.TimeoutError:
        process.kill()
        await process.wait()
        raise

    return (
        process.returncode or 0,
        stdout.decode(errors="replace"),
        stderr.decode(errors="replace"),
    )
# endregion

# region _is_valid_ip
def _isValidIp(value: str) -> bool:
    try:
        ipaddress.ip_address(value)
        return True
    except ValueError:
        return False
# endregion