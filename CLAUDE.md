# CLAUDE.md

Richtlinien für Claude Code bei der Arbeit an **WaveMP – a nostalgic media player**.

## Projekt

WaveMP ist ein selbst gehosteter Media Player für Musik und Musikvideos, ausgeliefert als Docker-Image. Die Optik orientiert sich an den klassischen Desktop-Media-Playern der Frutiger-Aero-Ära (Glas, Glanz, Hellblau, weiche Verläufe). Der Server scannt einen eingebundenen Medienordner, speichert die Bibliothek in einer Datenbank und stellt sie einer Web-App bereit. Native Clients (Android, iOS, Windows, macOS, evtl. Apple TV) folgen später und nutzen dieselbe API.

## Tech-Stack

| Bereich | Technologie |
|---|---|
| Backend | .NET 10, ASP.NET Core (REST-API) |
| Datenbank | PostgreSQL 18, EF Core mit Npgsql |
| Frontend | Vue 3, TypeScript, Vite, Pinia, Vue Router |
| Medien | ffmpeg / ffprobe (Scan, Transcoding, HLS), TagLib# (Tags) |
| Visualisierung | butterchurn (Milkdrop-Presets), eigene WebGL-Styles |
| Auslieferung | Docker-Image (GHCR + Docker Hub), docker compose |
| CI | GitHub Actions |

## Repository-Struktur

```
/assets                        Grafiken, Icons, Sounds und sonstige Dateien, die im Projekt verwendet werden
/source
  WaveMP.Service/              ASP.NET-Core-Host, Controller, DI-Setup
  WaveMP.Core.<Bereich>/       Geschäftslogik pro Bereich (z. B. Core.Library, Core.Scanning, Core.Users, Core.Streaming)
  WaveMP.Data/                 EF Core: Entities, DbContext, Migrations, Repositories
  WaveMP.Web/                  Vue-3-Frontend
/tests
  WaveMP.<Projekt>.Tests/      Unit-Tests, ein Testprojekt pro Quellprojekt
  WaveMP.E2E/                  End-to-End-Tests
/docs                          Dokumentation für Nutzer
WaveMP.slnx                    Solution
Dockerfile
docker-compose.yml
README.md
LICENSE
CLAUDE.md
```

Neue Projekte und Ordner fügen sich in diese Struktur ein. Abweichungen nur nach Rückfrage.

## Konventionen

### Code-Stil
- Im Repo-Root liegt eine `.editorconfig` des Maintainers. Sie ist verbindlich für allen Code (C#, TypeScript, Vue, JSON, YAML …).
- Die `.editorconfig` wird **nicht verändert**. Wenn eine Regel im Weg ist oder fehlt, nachfragen.
- Vor jedem Commit `dotnet format` ausführen; im Frontend die Formatierung an der `.editorconfig` ausrichten.

### .NET
- Projekt-Suffixe: `.Service` für den REST-Host, `.Data` für EF Core und Datenzugriff, `.Core.<Bereich>` für Geschäftslogik, aufgeteilt nach Aufgaben. Kein `.Application`.
- Klassen, die nur Extension-Methoden enthalten, heißen `*Extensions.cs` (Plural), z. B. `ServiceCollectionExtensions.cs`.
- Ordnernamen immer im Plural, außer Ordner, die Projektnamen abbilden.
- Tests: ausschließlich NUnit, AwesomeAssertions und NSubstitute. Keine weiteren Test-NuGets (z. B. kein Testcontainers, kein xUnit, kein Moq, kein FluentAssertions).
- API-Integrationstests laufen gegen eine echte PostgreSQL-Instanz, die lokal per docker compose und in der CI als Service-Container bereitgestellt wird.
- E2E-Tests der Web-App mit Playwright (npm).
- Etablierte, verbreitete NuGet-Pakete mit permissiver Lizenz sind Eigenbauten vorzuziehen.

### Frontend
- Composition API mit `<script setup lang="ts">`.
- API-Zugriffe laufen ausschließlich über einen mit Microsoft Kiota aus dem OpenAPI-Dokument generierten Client, nie direkt per `fetch` in Komponenten. Der generierte Code wird nicht von Hand bearbeitet.
- UI-Texte auf Englisch.

### API-Design
- REST, ressourcenorientiert, Substantive im Plural: `/api/v1/tracks`, `/api/v1/albums/{id}/tracks`.
- Korrekte HTTP-Verben (GET, POST, PUT, PATCH, DELETE) und Statuscodes (200, 201 + `Location`, 204, 400, 404, 409 …).
- Fehler als Problem Details nach RFC 9457.
- Paging, Sortierung und Filter über Query-Parameter; Collections sind immer gepaged.
- Versioniert unter `/api/v1`. Breaking Changes nur in einer neuen Version.
- Das OpenAPI-Dokument wird generiert und aktuell gehalten. Es ist der **einzige Vertrag** zwischen Server und Clients.
- Clients (Web-App und spätere native Apps mit .NET MAUI, Swift, Kotlin o. ä.) erhalten **keine Shared Libraries** aus diesem Projekt. Sie generieren ihre API-Schnittstellen aus dem OpenAPI-Dokument, bevorzugt mit Microsoft Kiota.
- Das OpenAPI-Dokument muss deshalb vollständig und generatorfreundlich sein: eindeutige `operationId`s, benannte Schemas, korrekt beschriebene Response-Typen und Statuscodes, Enums als benannte Typen.
- DTOs liegen im `.Service`-Projekt und werden nicht mit den Entities aus `.Data` vermischt.
- Medien-Streaming unterstützt HTTP-Range-Requests; transcodierte Inhalte laufen über HLS.
- Authentifizierung ist von Anfang an tokenbasiert, obwohl es keinen Login gibt: Die Auswahl eines Profils erzeugt ein Token. Native Clients nutzen das später genauso.
- Die Web-App darf nichts voraussetzen, was die API nicht bereitstellt.

## Produktregeln

- **Einfache Einrichtung nach Standard:** App und PostgreSQL laufen als zwei Container per docker compose, wie bei vergleichbaren Self-Hosting-Projekten üblich. Einzige Pflichtangabe ist `DB_PASSWORD` (per `.env`), die beide Container nutzen. PostgreSQL ist nur innerhalb des Docker-Netzwerks erreichbar. Alles andere hat sinnvolle Standardwerte.
- **Standards zuerst:** Wenn eine Vorgabe vom üblichen bzw. offiziell empfohlenen Vorgehen abweicht oder es eine bessere Lösung gibt, vor der Umsetzung darauf hinweisen und die Alternative nennen.
- **Benutzer:** kein Login, Profilauswahl ähnlich wie bei Streamingdiensten. Ein fest eingebautes Admin-Profil, das weniger prominent dargestellt wird, sobald es weitere Benutzer gibt. Optionale PIN später, per Umgebungsvariable (`ADMIN_PIN`) setzbar.
- **Datei-Gruppierung:** Alle Dateien eines Titels tragen denselben Basisnamen im selben Ordner (`Song.mp3`, `Song.mkv`, `Song.jpg`). Die Formate pro Kategorie dürfen abweichen. Verschiedene Ordnerstrukturen müssen unterstützt werden.
- **Duplikate:** Gibt es mehrere Audio- (oder Video-)Dateien mit gleichem Basisnamen, gewinnt die höchste Qualität. Eine Admin-Auswahl pro Titel kommt später.
- **Cover:** Standardmäßig Album-Cover (Ebene des Album-Ordners) vor Titel-Cover; später global, pro Album und pro Titel umstellbar.
- **Metadaten:** zuerst aus Tags, dann aus Ordner-/Dateistruktur, sonst bestmögliches Ergebnis. Änderungen durch den Admin werden **nur in Audiodateien** zurückgeschrieben, nie in Videos. Der Editor ist nur aktiv, wenn die Datei beschreibbar ist und eine Audiodatei existiert.
- **Wiedergabe:** Direct Play, wenn der Client das Format unterstützt, sonst Transcoding mit ffmpeg. Software-Transcoding muss immer funktionieren; Hardware-Beschleunigung (Intel Quick Sync über `/dev/dri`) ist optional.
- **Visualisierungen:** Die eigenen Styles heißen **A-Force** und **BlackCap**. Fremdprodukte, die als Vorbild dienen, werden nirgends genannt: nicht im Code, nicht in Kommentaren, Commits oder der Dokumentation.
- **Design:** nur eigene Grafiken. Keine Logos, Icons, Sounds oder sonstigen Assets von Microsoft.

## Lizenzen

- Das Projekt steht unter der MIT-Lizenz.
- Nur Abhängigkeiten mit MIT-kompatiblen Lizenzen (MIT, BSD, Apache-2.0, ISC, MPL-2.0, LGPL bei dynamischer Einbindung). **Keine GPL- oder AGPL-Abhängigkeiten** in NuGet oder npm.
- Vor dem Hinzufügen eines Pakets die Lizenz prüfen und in der Commit-Beschreibung nennen.
- ffmpeg wird als externer Prozess aufgerufen, nie gelinkt. Der Lizenzhinweis wird mit dem Image ausgeliefert.
- **Testmedien** (Audio, Video, Cover) werden nicht aus fremden Quellen übernommen, sondern zur Testlaufzeit synthetisch mit ffmpeg erzeugt (Sinustöne, Testbilder, generierte Cover, Tags per `-metadata`). Dateinamen und Ordnerstrukturen legen die Tests selbst an. Ausnahmen nur mit Medien unter CC0 und mit Herkunftsangabe in `/tests/assets/LICENSES.md`.

## Git

- Lokal committen. Commit-Nachrichten auf Englisch im Format `WMP-{Roadmap-Punkt}: {Nachricht}`, z. B. `WMP-5: add scan logic`. Die Nummer entspricht dem Roadmap-Schritt, an dem gerade gearbeitet wird.
- **Niemals pushen.** Gepusht wird immer manuell durch den Maintainer.
- `ROADMAP.md` existiert nur lokal und steht in der `.gitignore`. Niemals committen.

## Arbeitsweise

- Immer nur an einem Roadmap-Schritt arbeiten. Keine Features aus späteren Schritten umsetzen.
- Spätere Features (native Clients, PIN, Overrides) nur dort vorbereiten, wo es teure Umbauten vermeidet, und das ausdrücklich erwähnen.
- Jedes Feature enthält Tests für seine Geschäftslogik.
- `README.md` aktuell halten, wenn sich Setup oder Bedienung ändern.
- Keine Emojis in Dokumentation (README, CLAUDE.md, ROADMAP, /docs, CHANGELOG), Commit-Nachrichten und Code-Kommentaren.
- Bei Unklarheiten nachfragen statt raten.