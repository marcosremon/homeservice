from enum import IntEnum

class IntentName(IntEnum):
    """Los nombres de los miembros son los que manda Amazon tal cual.

    Son la unica excepcion al camelCase del proyecto: aqui el nombre ES el
    contrato con Amazon, no una variable nuestra.

    Ojo: el enrutado del AlexaService de C# hace Enum.TryParse SIN ignoreCase,
    asi que la busqueda tiene que ser exacta -> IntentName[intent.name].
    GeneralUtils.ParseEnum no sirve aqui: pasa el valor a mayusculas.
    """
    ConversationIntent = 0
    roomba_order_ = 1
    computer_status_order_ = 2
    light_order_ = 3