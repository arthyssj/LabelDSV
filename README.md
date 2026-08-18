# Etiquetas DSV - version .NET / WPF

Reescritura de la app de Python en C# + WPF. Misma logica de generacion
de ZPL, misma tecnica de impresion RAW via winspool.drv, interfaz nativa
de Windows en modo oscuro.

## Requisitos para compilar (una sola vez, en tu PC con Windows)

1. Instala el **SDK de .NET 8** (gratis):
   https://dotnet.microsoft.com/download/dotnet/8.0
   Durante la instalacion no se requieren permisos especiales de
   administrador si usas el instalador normal para tu usuario.

2. Verifica que quedo instalado, abre una terminal (PowerShell) y corre:

       dotnet --version

   Debe mostrar algo como "8.0.x".

## Como compilarlo y probarlo mientras editas

Parado en la carpeta del proyecto (donde esta EtiquetasDSV.csproj):

    dotnet build

Si compila sin errores, para correrlo directo sin generar el .exe final:

    dotnet run

## Como generar el .exe final (un solo archivo, sin instalar nada)

    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

El archivo queda en:

    bin\Release\net8.0-windows\win-x64\publish\EtiquetasDSV.exe

Ese unico .exe ya incluye el runtime de .NET adentro: se copia a
cualquier PC con Windows y corre con doble clic, sin instalar nada.
Pesa mas que la version framework-dependent (unos 60-150 MB) porque
carga el runtime completo, pero es la opcion mas segura para una PC
sin permisos de instalacion.

## Estructura del proyecto

    EtiquetasDSV.csproj   - configuracion del proyecto
    App.xaml / .cs        - punto de entrada, tema oscuro global (recursos)
    MainWindow.xaml        - diseno de las 3 pestanas (Individual, Lote, Config)
    MainWindow.xaml.cs     - logica de la ventana: eventos, impresion, lote
    ZplBuilder.cs           - construye la cadena ZPL de la etiqueta
    PrinterService.cs       - envia ZPL crudo a la impresora (P/Invoke winspool)
    VistaPrevia.cs           - dibuja la vista previa en el Canvas
    FilaLote.cs               - modelo de una fila de la tabla de lote
    Config.cs                  - configuracion persistente (JSON en el perfil)

## Nota importante

Este codigo se escribio sin poder compilarlo en el entorno donde se
genero (no tiene el SDK de .NET instalado). Es muy probable que
`dotnet build` marque algun error menor de sintaxis o un using
faltante la primera vez. Es normal: correlo, pega el error completo
en Claude Code o aqui mismo, y se corrige en un mensaje.

## Ajustar el diseno de la etiqueta

Las coordenadas del ZPL estan en `ZplBuilder.cs`, metodo `Construir()`.
Son identicas a las de la version en Excel/VBA, asi que cualquier ajuste
que ya hayas hecho ahi (mover un codigo de barras, cambiar un texto) se
puede replicar cambiando el mismo numero en este archivo.

La vista previa (`VistaPrevia.cs`) es una aproximacion visual, no un
render exacto del ZPL. Para verificar el diseno real antes de imprimir,
sigue usando labelary.com/viewer.html con el ZPL generado.
