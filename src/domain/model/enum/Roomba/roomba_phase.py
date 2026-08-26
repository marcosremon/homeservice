from enum import IntEnum

class RoombaPhase(IntEnum):
    CHARGE = 0
    RUN = 1
    STOP = 2
    HM_USR_DOCK = 3
    STUCK = 4