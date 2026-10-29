<<<<<<< HEAD
# Simulador de Básculas (Windows)

Simula básculas que hablan por TCP/IP. Hoy: **Radwag PUE 7.1 / HY10** (protocolo CBCP-03,
ver `..\Radwag-PUE71-Commands.md`). Reemplaza al simulador en ESP32: el puerto por defecto sigue
siendo el **4001** y las tramas de `S`, `OT`, `T`, `Z` son las del sketch.

## Uso

1. Instala con `dist\ScaleSimulator-Setup-1.0.0.exe` (por usuario, sin administrador).
2. Al abrir, ya hay una Radwag escuchando en `:4001`. La ventana muestra las direcciones IP de la PC
   para apuntar tu software (`192.168.x.x:4001`).
3. Pon carga con el campo, el deslizador, los botones ±, las cargas rápidas o «Aleatorio».
   El LCD muestra el peso **neto**; **Cero** y **Tara** equivalen a los comandos `Z` y `T`.
4. La **consola** muestra cada comando recibido (RX) y respondido (TX), con `\r\n` visibles.
5. **Ajustes**: puerto, unidad base, capacidad, decimales, plataformas (1–4), número de serie,
   tiempos de respuesta, «báscula ocupada» (`_I`) y el formato Radwag (signo `+`, marca `*` en la unidad).

«Lectura estable» desmarcada simula una báscula inestable: `S`/`SU`/`T`/`Z` esperan y responden `_E`
al agotar el tiempo; `SI`/`SUI`/`C1` responden con `?` y valores que oscilan.

## Compatibilidad con el sketch del ESP32

| Comando | Sketch | Simulador |
|---|---|---|
| `Z` | `Z A` `Z D` | igual (`Z_^` si la carga excede ±2 % de la capacidad) |
| `T` | `T A`, 1 s, `T D` | igual; tara = peso en plataforma (`T_v` si no hay carga) |
| `OT` | `OT_%9.2f_kg*_` con el peso | igual, con el valor de **tara** real |
| `S` | `S A`, 0.5 s, `S    +%9.2f_kg*` | igual, con el peso **neto** |

Diferencia deliberada: el sketch leía por `readString` y exigía el texto exacto; el simulador
separa por `\n`, acepta varios comandos en un mismo paquete y mayúsculas/minúsculas.
Implementados todos los comandos del manual: `Z T OT UT S SI SIA SU SUI C1 C0 CU1 CU0 DH UH ODH OUH SS P1–P4 NB SM RM BP OMI OMS OMG PC`.
Supuestos (el manual no los especifica): `SS` responde `SS_A` y `SS_D`; los modos de trabajo
disponibles son 1, 2, 3, 4, 11, 12 y 13.

## Arquitectura

```
ScaleSim.Core      Abstracciones y motor, sin nada específico de una marca
  Abstractions/    IScaleModel · IScaleProtocol · IProtocolSession · ScaleCatalog
  Model/           ScaleState (la báscula "física") · ScaleOptions · UnitCatalog · ActivityLog
  Networking/      TcpScaleServer (escucha, separa líneas, registra; el significado lo da el protocolo)
  ScaleInstance / ScaleManager / Storage   báscula dada de alta = modelo + ajustes + estado + servidor
ScaleSim.Radwag    RadwagModel + RadwagProtocol (CBCP-03)
ScaleSim.App       WPF + WebView2 que aloja la UI HTML (wwwroot/), igual que NotiLink
ScaleSim.Tests     xUnit: protocolo, tramas, TCP real
```

Los datos se guardan en `%LocalAppData%\ScaleSimulator\scales.json`.

### Agregar otra marca/modelo

1. Crea un proyecto `ScaleSim.<Marca>` con una clase que implemente `IScaleModel`
   (nombre, puerto por defecto, modos, `ExtraOptions` propias, valores por defecto) y un `IScaleProtocol`
   cuya sesión interprete una línea y responda con `IClientChannel.SendAsync`. Lee/modifica la báscula
   solo mediante `ScaleContext.State` (`Read`, `SetLoad`, `Zero`, `Tare`, …).
2. Regístralo en `MainWindow.xaml.cs`: `new ScaleCatalog().Register(new RadwagModel()).Register(new MiModelo())`.
3. La UI lo ofrece sola en «＋ Agregar», con sus opciones extra; se pueden tener varias básculas a la vez
   (cada una en su puerto).

Si un protocolo no termina sus líneas con `\n` (o es binario), el framing de `TcpScaleServer`
es el único punto a generalizar.

## Compilar

```powershell
dotnet test                       # pruebas
dotnet run --project ScaleSim.App/ScaleSimulator.App.csproj
.\build-installer.ps1             # dist\ScaleSimulator-Setup-<versión>.exe  (requiere Inno Setup 6)
```

### Firma y Smart App Control

Con Smart App Control activo, Windows bloquea ejecutables sin firma de confianza. `build-installer.ps1`
firma con un certificado local autofirmado (`CN=ScaleSimulator Dev Signing`, en `Cert:\CurrentUser\My`) y
deja la parte pública en `dist\ScaleSimulator-Signing.cer`. Para que **esta PC** (o cualquier otra) lo acepte,
ejecuta una vez `tools\trust-signing-cert.ps1`. Para distribuirlo fuera de tu equipo lo ideal es un certificado
de firma de código comercial.
=======
# ScaleSimulator
Aplicacion para simulacion de basculas
>>>>>>> 99f61f305a521e681266559970b1fbac0761b09b
