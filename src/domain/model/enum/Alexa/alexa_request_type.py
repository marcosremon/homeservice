from enum import IntEnum

class AlexaRequestType(IntEnum):
    """El nombre del miembro es el que manda Amazon tal cual.

    Igual que IntentName: la busqueda tiene que ser exacta
    -> AlexaRequestType[alexa_request_data.type].
    """
    LaunchRequest = 0