# Changelog

Historial condensado de cambios. El estado y arquitectura *actuales* viven en [README.md](README.md); este archivo es solo la bitácora histórica (reemplaza a `RESUMEN.md`, `CSGO_PRICE_FIX.md`, `DARK_MODE_README.md` y `UI_IMPROVEMENTS.md`, ahora eliminados).

## v0.1.0 — 2026-09-30

A partir de esta versión el versionado sigue [semver](https://semver.org/lang/es/); las entradas de abajo son historial previo a eso.

- **Arquitectura**: interfaces nuevas para los 5 servicios externos (`ICoinGeckoService`, `ILisSkinsService`, `IStockService`, `IImageCacheService`, `ICsvExportService`) y `IInvestmentService`; inyección de dependencias manual desde un composition root en `App.axaml.cs` en vez de que cada ViewModel haga `new XxxService()` por su cuenta.
- **Tests**: nuevo proyecto `Tests/HowsMyMoney.Tests` (xUnit + Moq) — 22 tests sobre los cálculos de `Investment`, `LisSkinsService` (sin red real, con `HttpMessageHandler` falso) e `InvestmentService` (SQLite real de prueba + mocks de los servicios externos).
- **Íconos de skins CS:GO**: nuevo `SteamIconService` resuelve el ícono real desde Steam Community Market una sola vez por skin (nunca para precios); queda persistido en `Investment.ImageUrl` y cacheado en SQLite como el resto de las imágenes.
- **Seguridad de datos**: eliminar una inversión ahora pide confirmación antes de borrar — antes el botón 🗑️ borraba directo y sin posibilidad de deshacer.
- **Dashboard — rediseño visual**: tarjetas de resumen flat con borde de acento (antes degradados con glow), header sin glassmorphism para que combine con el resto, y la lista de inversiones pasó de 4 secciones apiladas a pestañas por tipo de activo.
- **Limpieza de repo**: eliminados backups y scripts sueltos (`DashboardView.axaml.backup2`, `fix_dashboard_alignment.py`) y los 4 `.md` de bitácora histórica, consolidados en este changelog.

## v2.1 — Migración a .NET 8 y LIS-Skins API
- Actualizado el proyecto de `net7.0` (EOL) a `net8.0`.
- Reemplazado el scraping directo de Steam Community Market (`SkinportService`) por `LisSkinsService`, que usa el price list público de [LIS-Skins](https://lis-skins.stoplight.io/docs/lis-skins/dzq78x3edc19r-api-overview) (sin API key, cacheado 10 min en memoria).

## v2.0 — Dark Mode Edition (2025-11-07)
- Dark Mode activado por defecto (`App.axaml`, `Themes/DarkTheme.axaml`), paleta inspirada en VS Code.
- Eliminadas las sugerencias genéricas con precios falsos en la búsqueda de skins CS:GO.

## v1.2 — Optimización CS:GO (2025-11-07)
- Base de datos local ampliada (60+ skins populares), filtrado exclusivo de CS:GO (App ID 730).
- Búsqueda multinivel (exacta → parcial → palabras clave) y ordenamiento por precio más bajo.

## v1.1 — Mejoras de UI y búsqueda
- Rediseño de sidebar, dashboard y formulario de nueva inversión (tarjetas, gráficos, formularios con iconos y estilos consistentes).
- Búsqueda interactiva de activos con resultados seleccionables por clic.

## v1.0 — Release inicial
- Base del proyecto con Avalonia UI, modelos de datos y EF Core, servicios de API (CoinGecko, skins), dashboard con gráficos y exportación CSV.
