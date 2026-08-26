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
async def computer_status(ip_address: str) -> bool:
    """True si el equipo responde al ping."""
    try:
        if not _is_valid_ip(ip_address):
            return False

        if sys.platform == "win32":
            command = ["ping", "-n", "1", "-w", str(int(_PING_TIMEOUT_SECONDS * 1000)), ip_address]
        else:
            command = ["ping", "-c", "1", "-W", str(int(_PING_TIMEOUT_SECONDS) or 1), ip_address]

        exit_code, stdout, _ = await _run(command, timeout=_PING_TIMEOUT_SECONDS + 2)

        if exit_code != 0:
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
async def send_wake_on_lan_packet(mac_address: str, broadcast_ip: str) -> None:
    """Envia el magic packet directamente por UDP."""
    if not _MAC_PATTERN.match(mac_address):
        raise ValueError(f"MAC address invalida: {mac_address}")

    if not _is_valid_ip(broadcast_ip):
        raise ValueError(f"IP de broadcast invalida: {broadcast_ip}")

    mac_bytes = bytes.fromhex(mac_address.replace(":", "").replace("-", ""))
    magic_packet = b"\xff" * 6 + mac_bytes * 16

    def _send() -> None:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp_socket:
            udp_socket.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
            udp_socket.sendto(magic_packet, (broadcast_ip, _WAKE_ON_LAN_PORT))

    # sendto es bloqueante: fuera del event loop para no frenar otras peticiones.
    await asyncio.to_thread(_send)
# endregion

# region shutdown_computer
async def shutdown_computer(ip_address: str, ssh_user: str) -> str:
    is_on = await computer_status(ip_address)
    if not is_on:
        return "El ordenador ya esta apagado."

    exit_code, stderr = await _shutdown_via_ssh(ssh_user, ip_address)

    if exit_code != 0:
        raise RuntimeError(f"SSH poweroff fallo (exit {exit_code}): {stderr}")

    return "Apagando el ordenador (CachyOS via SSH)."
# endregion

# region _shutdown_via_ssh
async def _shutdown_via_ssh(ssh_user: str, ssh_host: str) -> tuple[int, str]:
    if not _is_valid_ip(ssh_host):
        return 1, f"Host SSH invalido: {ssh_host}"

    if not ssh_user.isidentifier():
        return 1, f"Usuario SSH invalido: {ssh_user}"

    command = [
        "ssh",
        "-o", "StrictHostKeyChecking=no",
        "-o", "BatchMode=yes",
        "-i", str(_SSH_KEY_PATH),
        f"{ssh_user}@{ssh_host}",
        "sudo systemctl poweroff",
    ]

    try:
        exit_code, _, stderr = await _run(command, timeout=_SSH_TIMEOUT_SECONDS)
        return exit_code, stderr
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
def _is_valid_ip(value: str) -> bool:
    try:
        ipaddress.ip_address(value)
        return True
    except ValueError:
        return False
# endregion