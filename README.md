# HowsMyMoney

Aplicación de escritorio multiplataforma para seguimiento de inversiones personales.
Permite registrar y monitorear:

- Criptomonedas
- Acciones y ETFs
- Skins de CS:GO
- Activos manuales

La app calcula valor actual, ganancia/pérdida, rendimiento porcentual y muestra un dashboard con resumen, gráficos y vistas específicas por mercado.

## Qué hace

- **Dashboard**: muestra el estado general del portafolio.
- **Monitoreo de mercados**: permite activar Crypto, Stocks y CS:GO desde una sola vista.
- **Actualización de precios**: refresca valores de mercado usando APIs públicas.
- **Persistencia local**: guarda inversiones e imágenes en SQLite para no volver a descargarlas.
- **Caché de imágenes**: descarga el icono una vez y luego lo reutiliza desde la base de datos.
- **Caché de precios**: evita repetir consultas innecesarias y reduce riesgo de rate limiting.

## Tecnologías

- `.NET 7`
- `Avalonia UI`
- `Entity Framework Core` + `SQLite`
- `CommunityToolkit.Mvvm`
- `LiveCharts`
- `SkiaSharp`

## Arquitectura general

```text
┌───────────────────────────────┐
│           MainWindow          │
└───────────────┬───────────────┘
                │
                v
┌───────────────────────────────┐
│      MainWindowViewModel      │
│  controla navegación y carga  │
└───────┬─────────┬───────┬─────┘
        │         │       │
        v         v       v
┌────────────┐ ┌──────────────┐ ┌──────────────────┐
│ Dashboard  │ │ AddInvestment │ │ MarketMonitor    │
│   View     │ │     View      │ │      View        │
└─────┬──────┘ └──────┬───────┘ └────────┬─────────┘
      │               │                  │
      v               v                  v
┌───────────────────────────────────────────────────┐
│                 ViewModels                        │
│ calculan estado, comandos y ordenan cargas         │
└───────────────┬───────────────────────┬───────────┘
                │                       │
                v                       v
┌───────────────────────────────┐  ┌───────────────────────────────┐
│          Services             │  │           SQLite              │
│ APIs, precios, imágenes,      │  │ inversiones + caché imágenes   │
│ exportación y persistencia     │  │                               │
└───────────────┬───────────────┘  └───────────────────────────────┘
                │
                v
┌───────────────────────────────────────────────────┐
│        APIs públicas / recursos remotos           │
└───────────────────────────────────────────────────┘
```

## Flujo de procesos

```text
Login -> Splash -> Dashboard
                │
                ├──> Carga inversiones desde DB
                ├──> Consulta precios de mercado
                ├──> Carga imágenes desde caché/DB
                ├──> Calcula métricas del portfolio
                └──> Actualiza gráficos y resumen

Dashboard -> Market Monitor
                │
                ├──> Crypto: consulta CoinGecko
                ├──> Stocks: consulta Yahoo Finance
                └──> CS:GO: consulta Steam Market / Skinport
```

## Diagrama de interacción entre componentes

```text
User
  │
  ▼
Views (.axaml)
  │ bindings / commands
  ▼
ViewModels
  │ orchestrate data flow
  ├─────────────► InvestmentService
  │                 ├─ CoinGeckoService
  │                 ├─ SkinportService
  │                 ├─ StockService
  │                 └─ ImageCacheService
  │
  ├─────────────► Dashboard calculations
  │
  └─────────────► MainWindowViewModel navigation
                    │
                    ▼
                  Views
```

## Consultas a nivel de red

### 1) Criptomonedas

Archivo: `Services/CoinGeckoService.cs`

Consultas usadas:

- `GET https://api.coingecko.com/api/v3/coins/markets`
- `GET https://api.coingecko.com/api/v3/simple/price`
- `GET https://api.coingecko.com/api/v3/coins/{id}`
- `GET https://api.coingecko.com/api/v3/search`

Uso:

- obtiene top cryptos
- obtiene precio individual cuando hace falta
- obtiene imagen y metadatos del activo
- busca monedas por texto

### 2) Acciones y ETFs

Archivo: `Services/StockService.cs`

Consultas usadas:

- `GET https://query2.finance.yahoo.com/v1/finance/search?q=...`
- `GET https://query1.finance.yahoo.com/v8/finance/chart/{symbol}`
- `GET https://logo.clearbit.com/{domain}.com`

Uso:

- busca acciones/ETFs por nombre o símbolo
- consulta el precio actual
- obtiene el logo de la empresa

### 3) Skins de CS:GO

Archivo: `Services/SkinportService.cs`

Consultas usadas:

- `GET https://steamcommunity.com/market/search/render/`
- `GET` a endpoints auxiliares de Steam Market para precios y resultados
- `GET https://community.cloudflare.steamstatic.com/economy/image/...`

Uso:

- busca items del market
- obtiene precio real
- obtiene imagen del item

### 4) Imágenes cacheadas

Archivo: `Services/ImageCacheService.cs`

Comportamiento:

- busca primero en `ImageCaches`
- si no existe, descarga la imagen
- si la descarga falla por SSL o red, genera un placeholder local
- guarda el resultado en SQLite
- reutiliza la imagen en siguientes arranques

### 5) Base de datos local

Archivo: `Data/InvestmentDbContext.cs`

Persistencia:

- `Investments`
- `PriceHistory`
- `ImageCaches`

La base de datos evita depender del estado de cada arranque y permite que los iconos ya descargados se reutilicen sin volver a consultar el origen.

## Flujo de carga de imágenes

```text
InvestmentViewModel
  │
  ├─ si ImageUrl está en caché -> usa bytes de SQLite
  │
  ├─ si ImageUrl no está en caché -> ImageCacheService descarga 1 vez
  │
  ├─ si falla SSL/red -> genera placeholder local y lo guarda
  │
  └─ entrega Bitmap a la UI
```

## Flujo de actualización de precios

```text
DashboardViewModel / MarketMonitorViewModel
  │
  ├─ llama InvestmentService o servicios de mercado
  │
  ├─ consulta API remota según tipo de activo
  │
  ├─ aplica caché / rate limit / throttling
  │
  └─ actualiza colecciones enlazadas a la UI
```

## Cómo correr la app

```bash
dotnet restore
dotnet build
dotnet run
```

## Estructura principal

```text
Models/        datos y entidades EF
Data/          contexto de SQLite
Services/      APIs, imágenes, exportación
ViewModels/    lógica MVVM
Views/         interfaces Avalonia
Migrations/    esquema de base de datos
```

## Notas de diseño

- La UI está pensada para ser responsive.
- Las vistas usan `WrapPanel`, `ScrollViewer` y grids con anchos definidos cuando hace falta mantener columnas alineadas.
- El dashboard prioriza consistencia visual y lectura rápida de métricas.

## Problemas que resuelve

- Evita descargar imágenes en cada inicio.
- Reduce llamadas innecesarias a APIs públicas.
- Mantiene precios y logos persistidos localmente.
- Unifica monitoreo de varios mercados en una sola app.

## Licencia

MIT# 💰 HowsMyMoney - Gestor de Inversiones Personales

![.NET](https://img.shields.io/badge/.NET-7.0-512BD4?logo=.net)
![Avalonia](https://img.shields.io/badge/Avalonia-11.3-8B44AC)
![License](https://img.shields.io/badge/license-MIT-green)

Una aplicación de escritorio multiplataforma para gestionar tus inversiones personales con soporte para criptomonedas, skins de CS:GO y activos manuales.

## 🎯 Características

- ✅ **Gestión de múltiples tipos de inversiones**
  - 💎 Criptomonedas con precios en tiempo real (CoinGecko API)
  - 🎮 Skins de CS:GO (Skinport API)
  - 📝 Activos manuales personalizados

- 📊 **Visualización avanzada**>
  - Dashboard con resumen completo del portfolio
  - Gráficos de rendimiento mensual
  - Distribución de activos por tipo
  - Cálculo automático de rentabilidad (ROI%)

- 💾 **Persistencia local**
  - Base de datos SQLite (sin configuración)
  - Historial de precios por mes
  - Exportación a CSV

- 🔄 **Actualización automática**
  - Sincronización de precios para criptos y skins
  - Tracking de rendimiento histórico

## 📸 Capturas de Pantalla

*Dashboard con inversiones y gráficos en tiempo real*

## 🏗️ Arquitectura Técnica

### Stack Tecnológico

| Componente | Tecnología | Versión |
|-----------|-----------|---------|
| **Framework** | .NET | 7.0 |
| **UI** | Avalonia UI | 11.3.8 |
| **Patrón** | MVVM | CommunityToolkit.Mvvm 8.2.1 |
| **Base de Datos** | SQLite + EF Core | 7.0.14 |
| **Gráficos** | LiveCharts2 | 2.0.0-rc4.5 |
| **APIs** | CoinGecko, Skinport | HTTP/JSON |
| **Exportación** | CsvHelper | 30.0.1 |

### Estructura del Proyecto

```
HowsMyMoney/
├── Models/                    # Modelos de datos
│   ├── AssetType.cs          # Enum de tipos de activo
│   ├── Investment.cs         # Modelo principal de inversión
│   └── PriceHistory.cs       # Historial de precios
├── Data/
│   └── InvestmentDbContext.cs # Contexto de Entity Framework
├── Services/                  # Lógica de negocio
│   ├── CoinGeckoService.cs   # API de criptomonedas
│   ├── SkinportService.cs    # API de skins CS:GO
│   ├── InvestmentService.cs  # Gestión de inversiones
│   └── CsvExportService.cs   # Exportación de datos
├── ViewModels/               # ViewModels MVVM
│   ├── MainWindowViewModel.cs
│   ├── DashboardViewModel.cs
│   └── AddInvestmentViewModel.cs
├── Views/                    # Interfaces de usuario
│   ├── MainWindow.axaml
│   ├── DashboardView.axaml
│   └── AddInvestmentView.axaml
└── Assets/                   # Recursos

Base de datos: ~/.local/share/HowsMyMoney/investments.db (Linux/macOS)
               %LOCALAPPDATA%/HowsMyMoney/investments.db (Windows)
```

## 🚀 Instalación y Uso

### Requisitos Previos

- **.NET 7.0 SDK** o superior
  - Descarga: https://dotnet.microsoft.com/download
  - Verifica la instalación: `dotnet --version`

### Instalación

1. **Clonar o descargar el proyecto**
   ```bash
   cd /ruta/a/HowsMyMoney
   ```

2. **Restaurar dependencias**
   ```bash
   dotnet restore
   ```

3. **Compilar el proyecto**
   ```bash
   dotnet build
   ```

4. **Ejecutar la aplicación**
   ```bash
   dotnet run
   ```

### Crear un ejecutable

Para generar un ejecutable independiente:

```bash
# Para tu plataforma actual
dotnet publish -c Release -r osx-arm64 --self-contained

# Para Windows
dotnet publish -c Release -r win-x64 --self-contained

# Para Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

El ejecutable estará en `bin/Release/net7.0/{runtime}/publish/`

## 📖 Guía de Uso

### 1. Agregar una Inversión

1. Haz clic en **"➕ Nueva Inversión"** en el sidebar
2. Selecciona el tipo de activo:
   - **Criptomoneda**: Busca por nombre (ej: Bitcoin)
   - **Skin CS:GO**: Busca por nombre (ej: AK-47 | Redline)
   - **Manual**: Para cualquier otro activo
3. Completa los campos:
   - Cantidad de unidades
   - Precio de compra
   - Fecha de compra
   - Notas opcionales
4. Haz clic en **"💾 Guardar Inversión"**

### 2. Ver el Dashboard

- **Resumen general**: Total invertido, valor actual, ganancia/pérdida
- **Gráfico mensual**: Evolución del portfolio en el tiempo
- **Distribución**: Porcentaje por tipo de activo
- **Lista detallada**: Todas las inversiones con métricas individuales

### 3. Actualizar Precios

- Haz clic en **"🔄 Actualizar Precios"** en el dashboard
- Se sincronizarán automáticamente:
  - Criptomonedas desde CoinGecko
  - Skins CS:GO desde Skinport
  - Activos manuales mantienen su precio

### 4. Exportar Datos

- Haz clic en **"💾 Exportar CSV"**
- El archivo se guardará en `~/Documents/HowsMyMoney_Export_YYYYMMDD_HHMMSS.csv`
- Incluye todas las métricas calculadas

## 🔧 Configuración Avanzada

### APIs Utilizadas

#### CoinGecko API (Criptomonedas)
- **Endpoint**: https://api.coingecko.com/api/v3
- **Sin API Key requerida** (limitado a 50 req/min)
- **Documentación**: https://www.coingecko.com/en/api

#### Skinport API (CS:GO Skins)
- **Endpoint**: https://api.skinport.com/v1
- **Sin API Key requerida** para datos públicos
- **Documentación**: https://docs.skinport.com

### Personalizar Base de Datos

Por defecto, la base de datos se guarda en:
- **macOS/Linux**: `~/.local/share/HowsMyMoney/investments.db`
- **Windows**: `%LOCALAPPDATA%\HowsMyMoney\investments.db`

Para cambiar la ubicación, edita `Data/InvestmentDbContext.cs`:

```csharp
var dbPath = Path.Combine("tu/ruta/personalizada", "investments.db");
```

## 🛠️ Desarrollo

### Comandos Útiles

```bash
# Compilar
dotnet build

# Ejecutar en modo desarrollo
dotnet run

# Limpiar y recompilar
dotnet clean && dotnet build

# Ejecutar tests (si los agregas)
dotnet test

# Generar migración de base de datos
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update
```

### Agregar Nuevas Características

El proyecto sigue el patrón **MVVM** (Model-View-ViewModel):

1. **Modelo**: Define en `Models/`
2. **Servicio**: Lógica de negocio en `Services/`
3. **ViewModel**: Estado y comandos en `ViewModels/`
4. **Vista**: UI en `Views/` (archivos .axaml)

Ejemplo: Para agregar un nuevo tipo de activo:
1. Actualiza `Models/AssetType.cs`
2. Crea un nuevo servicio en `Services/` si necesita API
3. Actualiza `Services/InvestmentService.cs` para manejar el nuevo tipo
4. La UI se adaptará automáticamente

## 📊 Métricas Calculadas

La aplicación calcula automáticamente:

- **Valor de Compra Total**: `Cantidad × Precio de Compra`
- **Valor Actual**: `Cantidad × Precio Actual`
- **Ganancia/Pérdida**: `Valor Actual - Valor de Compra`
- **Rentabilidad %**: `(Ganancia/Pérdida ÷ Valor de Compra) × 100`
- **Distribución por Tipo**: Porcentaje del portfolio por categoría
- **Rendimiento Mensual**: Valor total del portfolio agregado por mes

## 🐛 Solución de Problemas

### La aplicación no inicia
```bash
# Verifica la versión de .NET
dotnet --version

# Debe ser 7.0 o superior
# Si no, instala desde: https://dotnet.microsoft.com/download
```

### Errores de compilación
```bash
# Limpia y restaura
dotnet clean
dotnet restore
dotnet build
```

### No se actualizan los precios
- Verifica tu conexión a internet
- Las APIs públicas tienen límites de tasa
- CoinGecko: ~50 solicitudes por minuto
- Skinport: Consulta su documentación

### Base de datos corrupta
```bash
# Elimina la base de datos (perderás los datos)
# macOS/Linux:
rm ~/.local/share/HowsMyMoney/investments.db

# Windows:
# Elimina: %LOCALAPPDATA%\HowsMyMoney\investments.db
```

## 🤝 Contribuciones

Las contribuciones son bienvenidas! Para contribuir:

1. Haz un fork del proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Roadmap

Ideas para futuras versiones:

- [ ] Soporte para más exchanges (Binance, Kraken)
- [ ] Notificaciones de alertas de precio
- [ ] Modo oscuro
- [ ] Múltiples portfolios
- [ ] Importación desde CSV
- [ ] Sincronización en la nube
- [ ] Aplicación móvil (iOS/Android)
- [ ] Soporte para NFTs
- [ ] Calculadora de impuestos

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo `LICENSE` para más detalles.

## 👨‍💻 Autor

Desarrollado con ❤️ para gestionar inversiones personales de forma simple y efectiva.

## 🙏 Agradecimientos

- **CoinGecko** - API gratuita de criptomonedas
- **Skinport** - API de precios de skins CS:GO
- **Avalonia UI** - Framework multiplataforma
- **LiveCharts2** - Librería de gráficos
- **Entity Framework Core** - ORM para .NET

---

⭐ Si este proyecto te resulta útil, considera darle una estrella en GitHub!
