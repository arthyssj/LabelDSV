# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

EtiquetasDSV is a Windows desktop app (.NET 8 / WPF, C#) that generates and prints Zebra (ZPL) shipping labels for IN-BOND / DOMESTIC shipments. It's a rewrite of an original Python app, keeping the same RAW printing technique via `winspool.drv`. UI is a custom dark theme (no native Windows dialogs/controls).

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

- **`App.xaml` / `App.xaml.cs`** — entry point. `App.xaml` centralizes the entire dark theme as `Application`-level resources: color palette (`BgWindow`, `BgPanel`, `AccentGreen`, ...), `PanelStyle` (rounded border + `DropShadowEffect`), and *fully retemplated* `Button`/`ComboBox`/`TabControl`/`TabItem`/`DataGrid`/`ScrollBar` styles. Any new control that shows up with system-default white styling needs a full `ControlTemplate` replacement here — setting `Background`/`Foreground` alone does not repaint WPF's internal chrome (this is why the ComboBox popup/items, the TabControl header strip, and the ScrollBar track/thumb were retemplated).
- **`MainWindow.xaml` / `.cs`** — the 4-tab UI (Individual print, Batch print, Pallet batch print, Config) and all UI event/validation/print-orchestration logic. As of the printer-centralization change, there is a **single** printer `ComboBox` (`CmbImpresora`), living only in the Config tab — Individual/Lote/Pallet no longer have their own printer selector or refresh button; all three read `_cfg.Impresora` directly at print time.
- **`ZplBuilder.cs`** (`Construir()`) — the single source of truth for every real printed coordinate. Label is 4x6.5in @ 300dpi (`^PW1200` x `^LL1950`), content rotated 90° (`^A0R` text, `^BCR` barcodes) because the label reads sideways relative to how it feeds into the printer. Part Number, Quantity and Reference are plain anchored text (no `^GB` box, no `^FB` centering) — only the Tipo field still sits inside a box. There is no "From" block anymore (removed entirely, see below). The same file also has `ConstruirPallet()`, a separate template for the Pallet tab (see below).
- **`VistaPrevia.cs`** — draws an approximate on-screen preview on a `Canvas` by taking the *same* `^FOx,y`/`^GBancho,alto` values from `ZplBuilder` and applying the rotation transform `vistaX = zplY * escala`, `vistaY = (1200 - zplX) * escala` (swap width/height for rectangles). **Any coordinate change in `ZplBuilder.cs` must be manually mirrored here with the same formula** — the preview does not derive from ZplBuilder automatically, and there is no compiler check that they stay in sync. Preview is not pixel-perfect (barcodes render as solid black bars); verify real ZPL output on labelary.com/viewer.html.
- **`PrinterService.cs`** — sends the ZPL string as RAW print-queue data straight through Windows spooler via P/Invoke to `winspool.drv` (`OpenPrinter`/`StartDocPrinter`/`WritePrinter`), bypassing `System.Drawing.Printing`. `ListarImpresoras()` (via `EnumPrinters`) populates the single printer combo in the Config tab.
- **`CustomMessageBox.xaml` / `.cs`** — themed modal dialog replacing `System.Windows.MessageBox`, which ignores the app's dark theme.
- **`FilaLote.cs`** — `INotifyPropertyChanged` model for one row of the batch-print `DataGrid` (Individual/Lote tab). Enforces character limits in the property setters themselves (`Parte` 30, `Cantidad` 10, `Referencia` 18, `Notas` 55, truncating silently) as a backstop for paths that bypass the `DataGrid`'s `MaxLength` (paste from Excel, "Duplicar fila", "Generar refs").
- **`FilaPallet.cs`** — same `INotifyPropertyChanged` pattern as `FilaLote`, for one row of the Pallet batch `DataGrid`: `Pallet` (no limit) and `Referencia` (truncated to 18 chars in the setter, same backstop reasoning).
- **`Config.cs`** — persistent settings (title, date format, last printer, default copy count, reference-counter state) serialized to JSON at `%USERPROFILE%\etiquetas_dsv_config.json` via `Config.Cargar()`/`Config.Guardar()`. Falls back to defaults silently if the file is missing/corrupt. Also owns `GenerarReferencia()` (see below). No longer has `FromLinea1`/`FromLinea2` — the "From" address block was removed from the app entirely (config, UI and ZPL).

### Editing the label layout

Coordinates live in `ZplBuilder.cs::Construir()` and mirror the validated `labelMaster.txt` reference template — replicate any layout tweak made there by changing the same numeric coordinate here, then propagate the change to `VistaPrevia.cs` using the rotation formula above so the preview stays truthful.

Current column layout (x = `^FO` first coordinate, separators are the `^GB4,1830,4` vertical rules): Header `1070–1200` → Part Number `735–1070` → Quantity `425–735` → Reference **and** Tipo (share a column, Tipo sits lower along y) `135–425` → Notes/Date `0–135`. There is no "From" column anymore; removing it is what freed up space for Notes/Date to widen from `100` to `135`.

### Pallet tab

The Pallet tab is a **batch** tab, mirroring "Imprimir lote": a `DataGrid` (`GridPallet`, backed by `ObservableCollection<FilaPallet>`) with Pallet/Reference columns, paste-from-Excel, add/duplicate/delete/clear rows, and a background print loop with progress bar + cancel button. `ZplBuilder.cs::ConstruirPallet()` builds each label — it reproduces `zpl_pallet.txt` as-is, substituting only the variable fields (title from `cfg.Titulo`, pallet number, reference, date, copies). Unlike the main label, this one is **not rotated** (`^A0N`/`^BCN` instead of `^A0R`/`^BCR`), so it has no `VistaPrevia` counterpart. Reference on this tab is still always manual (no auto-generate control) — this was tried and explicitly reverted, and that decision carried over into the batch redesign.

### Batch printing

Both the "Imprimir lote" and "Pallet" tabs support pasting rows copied from Excel, tab-separated in order: Part Number, Quantity, Reference, Tipo, Notes (Lote) or Pallet, Reference (Pallet). Paste works two ways: the "Pegar" button, and **Ctrl+V** while the grid has focus — both call the same clipboard-parsing method (`PegarDelPortapapeles()` / `PegarDelPortapapelesPallet()`). The Ctrl+V binding is a `PreviewKeyDown` handler on the `DataGrid` (`GridLote_PreviewKeyDown` / `GridPallet_PreviewKeyDown`), **not** WPF's built-in paste command: the grids start with zero rows and `CanUserAddRows="False"`, so WPF's native per-cell Ctrl+V (which only overwrites existing cells) has nothing to paste into and silently does nothing. The `PreviewKeyDown` handler intercepts the key combo before that built-in handling runs and always adds new rows instead, regardless of how many rows currently exist.

Both batch prints run in the background with a progress bar and cancel button (separate `CancellationTokenSource` per tab: `_ctsLote` / `_ctsPallet`). Rows can also be duplicated ("Duplicar fila" clones the selected row and inserts it right after) and column headers are not clickable-to-sort (`CanUserSortColumns="False"`) so pasted row order is never silently reshuffled.

### Character limits

Enforced in two places for every limited field, so it holds regardless of entry path (typing, paste, duplicate, auto-generate): `MaxLength` on the `TextBox`/`DataGridTextColumn.EditingElementStyle` in XAML, **and** truncation in the row model's property setter (`FilaLote`/`FilaPallet`). Current limits: Part Number 30, Quantity 10, Reference 18, Notes 55. The Individual tab also shows a live `n/max` counter under each limited field (`TxtContadorParte`/`Cantidad`/`Referencia`/`Notas`, updated from `Campo_Cambiado`); the Lote/Pallet tabs show a static hint line above the grid instead, since a live per-cell counter isn't practical in a `DataGrid`.

### Automatic reference numbers

`Config.cs::GenerarReferencia()` builds `"RF" + diaSemana(2 digits, VBA-style: domingo=1…sabado=7) + DateTime.Now.ToString("MMddyyyy") + "-" + contador` (e.g. `RF0308182026-1`). The counter (`UltimoContadorReferencia`) auto-resets to 1 whenever the stored `UltimaFechaReferencia` differs from today, **and** whenever it would exceed `Config.MaxContadorReferencia` (999) — so the generated reference (`RF` + 2 + 8 + `-` + up to 3 digits = max 16 chars) never exceeds the 18-char Reference limit above. Both fields persist through `Config.Guardar()` so the sequence survives app restarts.

- **Individual tab** — a "Automatica" checkbox next to `TxtReferencia` makes the field read-only and fills it via `GenerarReferencia()`; a "Generar" button gets a fresh one without unchecking/rechecking.
- **Lote tab** — "Generar refs" fills `Referencia` only on rows that are currently blank, so it never overwrites a manually typed or Excel-pasted reference.
- **Pallet tab** — reference is always manual (no auto-generate control) — this was tried and explicitly reverted.
- **Config tab** — "Reiniciar contador" lets you force what the *next* generated number will be (e.g. type `100` to make the next reference `RF...-100`, capped at `1`–`999`); it sets `UltimoContadorReferencia = valor - 1` and `UltimaFechaReferencia = hoy` so the daily-reset logic doesn't immediately undo it. Needed because testing runs the counter up.

### Printer selection

There is exactly one printer setting in the whole app: `CmbImpresora` in the Config tab, plus its "Actualizar lista de impresoras" button. Selecting a printer saves it immediately (`CmbImpresora_SelectionChanged` sets `_cfg.Impresora` and calls `Config.Guardar()` right away) — it does **not** require clicking "Guardar configuracion" to take effect. Individual, Lote and Pallet all read `_cfg.Impresora` directly when printing; if it's empty, they show "Configura una impresora en la pestaña Configuracion." instead of trying to print. Don't reintroduce a per-tab printer `ComboBox` — that duplication (and the risk of the three combos disagreeing) is exactly what this change removed.
