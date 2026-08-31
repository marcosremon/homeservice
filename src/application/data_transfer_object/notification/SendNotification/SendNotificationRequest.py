from dataclasses import dataclass

@dataclass
class SendNotificationRequest:
    title: str = ""
    message: str = ""
    tags: str = ""