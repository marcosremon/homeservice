"""Equivalente a Transversal.Common.DatabaseMigration.Migration."""

from dataclasses import dataclass, field

@dataclass
class Migration:
    version: int = 0
    commands: list[str] = field(default_factory=list)