"""Datenmodelle für Kartendecks (JSON-Schema)."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

CardColor = Literal["red", "blue", "green", "gold", "purple", "grey"]


class Tracker(BaseModel):
    """Eine Reihe abstreichbarer Kreise auf einer Karte."""

    model_config = ConfigDict(extra="forbid")

    label: str = ""
    count: int = Field(default=0, ge=0, le=20)


class TrackerRow(Tracker):
    """Hervorgehobene Tracker-Zeile (z. B. für Zauberplätze)."""


class Card(BaseModel):
    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    name: str
    type: CardColor = "grey"
    cost: str = ""
    range_: str = Field(default="", alias="range")
    effect: str = ""
    fluff: str = ""
    tactic: str = ""
    tracker: Tracker | None = None
    tracker_rows: list[TrackerRow] = Field(default_factory=list)

    @model_validator(mode="before")
    @classmethod
    def _accept_legacy_keys(cls, data: Any) -> Any:
        """Erlaubt das alte Notebook-Format mit ``trackers``/``tracker_label``."""
        if not isinstance(data, dict):
            return data
        data = dict(data)
        count = data.pop("trackers", None)
        label = data.pop("tracker_label", None)
        if count and not data.get("tracker"):
            data["tracker"] = {"label": label or "", "count": count}
        return data


class PageSetup(BaseModel):
    model_config = ConfigDict(extra="forbid")

    width: str = "70mm"
    height: str = "120mm"
    margin: str = "4mm"


class CardDeck(BaseModel):
    model_config = ConfigDict(extra="forbid")

    title: str = "DnD Karten"
    page: PageSetup = Field(default_factory=PageSetup)
    cards: list[Card] = Field(default_factory=list)

    @model_validator(mode="before")
    @classmethod
    def _accept_bare_list(cls, data: Any) -> Any:
        """Erlaubt eine JSON-Datei, die nur aus einer Kartenliste besteht."""
        if isinstance(data, list):
            return {"cards": data}
        return data

    @classmethod
    def from_json(cls, raw: str | bytes) -> "CardDeck":
        return cls.model_validate(json.loads(raw))

    @classmethod
    def from_file(cls, path: str | Path) -> "CardDeck":
        return cls.from_json(Path(path).read_text(encoding="utf-8"))

    def to_json(self) -> str:
        return json.dumps(
            self.model_dump(by_alias=True, exclude_defaults=True),
            indent=2,
            ensure_ascii=False,
        )
