# DEV Funktion — Phantombite AdminProjektor

Stand: 2026-09-19 · Version 1.0.0 · Workshop-ID 3706769805 · Core-Kanal 1995011

## Zweck
Projektor-Block für Admins. Ein Klick auf **„Fertig bauen“** baut die aktuelle Projektion sofort fertig
und repariert danach alle Blöcke des Grids (und der mechanisch verbundenen Grids) auf 100 %.
Nützlich, um große Stationen und Prefabs auf dem Server aufzubauen, ohne von Hand zu schweißen.

## Blöcke
| SubtypeId | Größe | Besonderheit |
|---|---|---|
| `AdminProjektor_Large` | Large | benötigt 1000× `AdminChip` |
| `AdminProjektor_Small` | Small | benötigt 1000× `AdminChip` |

Der `AdminChip` stammt aus **Phantombite_Core**. Er ist nicht herstellbar, deshalb können nur
Admins den Block bauen. Wer Zugriff auf einen gebauten Block hat, kann den Knopf aber bedienen.

## Dateien
```
Data/CubeBlocks_AdminProjektor.sbc
Data/Scripts/AdminProjektor/
  Adminprojektor session.cs   Session: Core-Anbindung (READY, LOGLEVEL, PERFLEVEL), statische Werte
  AdminProjektor.cs           Block-Logik (Terminal-Controls, Build- und Repair-Phasen)
```

## Ablauf
1. **Build:** pro Frame werden baubare Blöcke der Projektion sofort gebaut (`Build`, `requestInstant`).
2. **Pause (120 Frames):** wartet, bis die neuen Blöcke im Grid registriert sind.
3. **Repair:** pro Frame werden Blöcke ins Stockpile gefüllt und geschweißt (`IncreaseMountLevel`), bis alles voll ist.

## Core-Anbindung
- Meldet sich als `adminprojektor` an (keine Commands). Empfängt `LOGLEVEL` und `PERFLEVEL`.
- Performance-Level bestimmt die Menge pro Frame (Build/Repair): 0 → 5/10, 1 → 2/5, 2 → 1/2, 3 → 1/1.
- Log-Meldungen der Block-Logik werden nach dem Log-Level des Core gefiltert.

## Offene Punkte / Roadmap
- [ ] Angleichen an `0_Phantombite_MOD_TEMPLATE.md` (README, patch_notes, thumb.jpg)
- [ ] Bricht ohne Meldung ab, wenn der Block keinen Besitzer hat (`ownerId == 0` in Phase 1)
- [ ] `NeedsUpdate = EACH_100TH_FRAME` in `Init` wird nicht gebraucht (Update läuft nur bei aktiver Arbeit)
