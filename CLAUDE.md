# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

EtiquetasDSV is a Windows desktop app (.NET 8 / WPF, C#) that generates and prints Zebra (ZPL) shipping labels for IN-BOND / DOMESTIC shipments. It's a rewrite of an original Python app, keeping the same ZPL generation logic and the same RAW printing technique via `winspool.drv`. UI is a custom dark theme (no native Windows dialogs/controls).

## Commands

Run from the folder containing `EtiquetasDSV.csproj`:

```
dotnet build                                                # compile
dotnet run                                                  # run without publishing
dotnet publish -c Release -p:PublishProfile=FolderProfile   # single standalone exe (VS "Publish" uses the same profile)
```

The published single-file exe lands at `bin\Release\net8.0-windows\win-x64\publish\EtiquetasDSV.exe`. There is no test suite in this repo.

`Properties\PublishProfiles\FolderProfile.pubxml` sets `SelfContained`, `PublishSingleFile`, `PublishReadyToRun` **and** `IncludeNativeLibrariesForSelfExtract` (all `true`). That last flag matters: WPF's native interop DLLs (`PresentationNative_cor3.dll`, `wpfgfx_cor3.dll`, `D3DCompiler_47_cor3.dll`, `vcruntime140_cor3.dll`, `PenImc_cor3.dll`) normally can't be embedded in a single-file publish and end up as loose files beside the exe — without `IncludeNativeLibrariesForSelfExtract` the app crashes with `DllNotFoundException` the moment the exe is copied anywhere without them. With it, .NET self-extracts those DLLs to a temp folder on first run and the exe is truly standalone. Don't remove that flag or use a bare `dotnet publish -p:PublishSingleFile=true` without it.

## Architecture

Single WPF project, no layered architecture — a handful of top-level classes:

- **`App.xaml` / `App.xaml.cs`** — entry point. `App.xaml` centralizes the entire dark theme as `Application`-level resources: color palette (`BgWindow`, `BgPanel`, `AccentGreen`, ...), `PanelStyle` (rounded border + `DropShadowEffect`), and *fully retemplated* `Button`/`ComboBox`/`TabControl`/`TabItem`/`DataGrid` styles. Any new control that shows up with system-default white styling needs a full `ControlTemplate` replacement here — setting `Background`/`Foreground` alone does not repaint WPF's internal chrome (this is why ComboBox popup/items and the TabControl header strip were retemplated).
- **`MainWindow.xaml` / `.cs`** — the 4-tab UI (Individual print, Batch print, Pallet print, Config) and all UI event/validation/print-orchestration logic.
- **`ZplBuilder.cs`** (`Construir()`) — the single source of truth for every real printed coordinate. Label is 4x6.5in @ 300dpi (`^PW1200` x `^LL1950`), content rotated 90° (`^A0R` text, `^BCR` barcodes) because the label reads sideways relative to how it feeds into the printer. The same file also has `ConstruirPallet()`, a separate template for the Pallet tab (see below).
- **`VistaPrevia.cs`** — draws an approximate on-screen preview on a `Canvas` by taking the *same* `^FOx,y`/`^GBancho,alto` values from `ZplBuilder` and applying the rotation transform `vistaX = zplY * escala`, `vistaY = (1200 - zplX) * escala` (swap width/height for rectangles). **Any coordinate change in `ZplBuilder.cs` must be manually mirrored here with the same formula** — the preview does not derive from ZplBuilder automatically, and there is no compiler check that they stay in sync. Preview is not pixel-perfect (barcodes render as solid black bars); verify real ZPL output on labelary.com/viewer.html.
- **`PrinterService.cs`** — sends the ZPL string as RAW print-queue data straight through Windows spooler via P/Invoke to `winspool.drv` (`OpenPrinter`/`StartDocPrinter`/`WritePrinter`), bypassing `System.Drawing.Printing`. `ListarImpresoras()` (via `EnumPrinters`) populates the printer combo boxes on both the Individual and Batch tabs.
- **`CustomMessageBox.xaml` / `.cs`** — themed modal dialog replacing `System.Windows.MessageBox`, which ignores the app's dark theme.
- **`FilaLote.cs`** — `INotifyPropertyChanged` model for one row of the batch-print `DataGrid`.
- **`Config.cs`** — persistent settings (title, two "From" address lines, date format, last printer, default copy count, reference-counter state) serialized to JSON at `%USERPROFILE%\etiquetas_dsv_config.json` via `Config.Cargar()`/`Config.Guardar()`. Falls back to defaults silently if the file is missing/corrupt. Also owns `GenerarReferencia()` (see below).

### Editing the label layout

Coordinates live in `ZplBuilder.cs::Construir()` and mirror the validated Excel/VBA template — replicate any layout tweak made there by changing the same numeric coordinate here, then propagate the change to `VistaPrevia.cs` using the rotation formula above so the preview stays truthful.

### Pallet tab

`ZplBuilder.cs::ConstruirPallet()` is a second, independent template that reproduces `zpl_pallet.txt` as-is, substituting only the variable fields (title from `cfg.Titulo`, pallet number, reference, date, copies). Unlike the main label, this one is **not rotated** (`^A0N`/`^BCN` instead of `^A0R`/`^BCR`), so it has no `VistaPrevia` counterpart and the Pallet tab has no preview panel — edits to its coordinates only need to happen in `ConstruirPallet()`.

### Batch printing

The "Imprimir lote" tab supports pasting rows copied from Excel (Ctrl+V or "Pegar"), tab-separated in order: Part Number, Quantity, Reference, Tipo, Notes. Batch printing runs in the background with a progress bar and cancel button. Rows can also be duplicated ("Duplicar fila" clones the selected `FilaLote` and inserts it right after) and column headers are not clickable-to-sort (`CanUserSortColumns="False"` on `GridLote`) so pasted row order is never silently reshuffled.

### Automatic reference numbers

`Config.cs::GenerarReferencia()` builds `"RF" + diaSemana(2 digits, VBA-style: domingo=1…sabado=7) + DateTime.Now.ToString("MMddyyyy") + "-" + contador` (e.g. `RF0308182026-1`). The counter (`UltimoContadorReferencia`) auto-resets to 1 whenever the stored `UltimaFechaReferencia` differs from today, and both fields persist through `Config.Guardar()` so the sequence survives app restarts.

- **Individual tab** — a "Automatica" checkbox next to `TxtReferencia` makes the field read-only and fills it via `GenerarReferencia()`; a "Generar" button gets a fresh one without unchecking/rechecking.
- **Lote tab** — "Generar refs" fills `Referencia` only on rows that are currently blank, so it never overwrites a manually typed or Excel-pasted reference.
- **Pallet tab** — reference is always manual (no auto-generate control) — this was tried and explicitly reverted.
- **Config tab** — "Reiniciar contador" lets you force what the *next* generated number will be (e.g. type `100` to make the next reference `RF...-100`); it sets `UltimoContadorReferencia = valor - 1` and `UltimaFechaReferencia = hoy` so the daily-reset logic doesn't immediately undo it. Needed because testing runs the counter up.
