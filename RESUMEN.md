# 📝 Resumen de la Aplicación HowsMyMoney

## ✅ Proyecto Completado

**HowsMyMoney** es una aplicación de escritorio multiplataforma construida con **.NET 7** y **Avalonia UI** para gestionar inversiones personales con soporte para:
- 💎 Criptomonedas (API de CoinGecko)
- 🎮 Skins de CS:GO (Base de datos local + fallback a API)
- 📝 Activos manuales

---

## 🏗️ Arquitectura

### Stack Tecnológico
```
Frontend:    Avalonia UI 11.3.8 (multiplataforma)
Backend:     .NET 7.0
Base de Datos: SQLite con Entity Framework Core 7.0
Patrón:      MVVM (CommunityToolkit.Mvvm)
Gráficos:    LiveCharts2 (SkiaSharp)
Exportación: CsvHelper
```

### Estructura del Proyecto
```
HowsMyMoney/
├── Models/              # Entidades de datos
│   ├── AssetType.cs
│   ├── Investment.cs
│   └── PriceHistory.cs
├── Data/                # Contexto de base de datos
│   └── InvestmentDbContext.cs
├── Services/            # Lógica de negocio
│   ├── CoinGeckoService.cs      # API de criptomonedas
│   ├── SkinportService.cs       # Skins CS:GO (SOLO App ID 730)
│   ├── InvestmentService.cs     # Gestión de inversiones
│   └── CsvExportService.cs      # Exportación de datos
├── ViewModels/          # ViewModels MVVM
│   ├── MainWindowViewModel.cs
│   ├── DashboardViewModel.cs
│   └── AddInvestmentViewModel.cs
└── Views/               # UI (AXAML)
    ├── MainWindow.axaml
    ├── DashboardView.axaml
    └── AddInvestmentView.axaml
```

---

## 🔑 Características Clave

### 1. Gestión de Criptomonedas
- ✅ Búsqueda en tiempo real usando CoinGecko API (gratuita)
- ✅ Actualización automática de precios
- ✅ Soporta 10,000+ criptomonedas
- ✅ Sin API key requerida

### 2. Gestión de Skins CS:GO
- ✅ **Base de datos local con 60+ skins populares**
- ✅ **SOLO items de Counter-Strike: Global Offensive (App ID: 730)**
- ✅ **NO incluye items de otros juegos (Dota 2, TF2, Rust, etc.)**
- ✅ Búsqueda por texto parcial (ej: "AK-47", "Redline", "Asiimov")
- ✅ Muestra TODAS las variantes que coincidan con la búsqueda
- ✅ **Ordenado por precio más bajo primero (Lowest Price)**
- ✅ Incluye múltiples calidades: Factory New, Minimal Wear, Field-Tested, Well-Worn, Battle-Scarred
- ✅ Fallback a CSGOBackpack API si no está en base de datos local

**Skins incluidas:**
- AK-47: Redline, Vulcan, Fire Serpent, Asiimov, Neon Rider
- AWP: Asiimov, Dragon Lore, Hyper Beast, Lightning Strike, Wildfire
- M4A4: Howl, Asiimov, Neo-Noir
- M4A1-S: Cyrex, Hyper Beast, Golden Coil
- Pistolas: Desert Eagle Blaze, Glock Fade, USP-S Kill Confirmed
- Cuchillos: Karambit, Butterfly, Bayonet (Fade, Doppler, Tiger Tooth)
- Otros: P90 Asiimov, etc.

### 3. Dashboard Interactivo
- ✅ Resumen del portfolio (Total Invertido, Valor Actual, Ganancia/Pérdida)
- ✅ Gráfico de rendimiento mensual (LineChart)
- ✅ Gráfico de distribución por tipo de activo (PieChart)
- ✅ Lista detallada con DataGrid
- ✅ Actualización de precios con un clic

### 4. Exportación de Datos
- ✅ Exporta a CSV con todas las métricas
- ✅ Compatible con Excel y Google Sheets
- ✅ Incluye: Nombre, Tipo, Cantidad, Precios, Ganancia/Pérdida, ROI%

### 5. Base de Datos Local
- ✅ SQLite (sin configuración, portable)
- ✅ Entity Framework Core con migraciones
- ✅ Historial de precios mensual
- ✅ Ubicación: `~/.local/share/HowsMyMoney/investments.db` (macOS/Linux)

---

## 🚀 Cómo Usar

### Instalación
```bash
cd /Users/josesalcedo/Desktop/HowsMyMoney
dotnet restore
dotnet build
dotnet run
```

### Agregar una Inversión

**Criptomoneda:**
1. Selecciona "Criptomoneda"
2. Escribe el nombre (ej: "Bitcoin")
3. Haz clic en 🔍 Buscar
4. Selecciona de la lista
5. El precio actual se obtiene automáticamente
6. Ingresa cantidad y precio de compra
7. 💾 Guardar

**Skin CS:GO:**
1. Selecciona "SkinCSGO"
2. Escribe parte del nombre (ej: "AK-47" o "Redline" o "Asiimov")
3. Haz clic en 🔍 Buscar
4. Se muestran TODAS las skins que coincidan, **ordenadas por precio más bajo**
5. Selecciona la que quieras (ej: "AK-47 | Redline (Field-Tested) - $15.00 (Lowest)")
6. El **precio más bajo** se establece automáticamente
7. Ajusta cantidad si es necesario
8. 💾 Guardar

**Activo Manual:**
1. Selecciona "Manual"
2. Ingresa todos los datos manualmente
3. 💾 Guardar

### Actualizar Precios
- Haz clic en "🔄 Actualizar Precios" en el Dashboard
- Solo actualiza criptomonedas y skins (los manuales no cambian)

---

## 📊 Métricas Calculadas

| Métrica | Fórmula |
|---------|---------|
| **Valor de Compra** | `Cantidad × Precio de Compra` |
| **Valor Actual** | `Cantidad × Precio Actual` |
| **Ganancia/Pérdida** | `Valor Actual - Valor de Compra` |
| **ROI %** | `(Ganancia/Pérdida ÷ Valor de Compra) × 100` |

---

## 🐛 Mejoras Implementadas

### v1.0 - Release Inicial
✅ Base del proyecto con Avalonia UI  
✅ Modelos de datos e EF Core  
✅ Servicios de API (CoinGecko, Skinport)  
✅ Dashboard con gráficos  
✅ Exportación CSV  

### v1.1 - Mejoras de Búsqueda
✅ Búsqueda interactiva con resultados en tiempo real  
✅ Selección de resultados con clic  
✅ Debugging mejorado con logs en consola  
✅ Notificación de cambios de propiedad para UI reactiva  

### v1.2 - Optimización CS:GO
✅ **Base de datos local ampliada (60+ skins populares)**  
✅ **Filtrado EXCLUSIVO de CS:GO (App ID: 730)**  
✅ **Búsqueda por texto parcial mejorada**  
✅ **Muestra TODAS las variantes que coincidan**  
✅ **Ordenamiento por precio más bajo (Lowest)**  
✅ **Múltiples calidades por skin**  
✅ **Validación de que solo se muestren items de CS:GO**  

---

## 🔧 Comandos Útiles

```bash
# Ejecutar
dotnet run

# Compilar Release
dotnet build -c Release

# Limpiar
dotnet clean

# Publicar para macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained

# Publicar para Windows
dotnet publish -c Release -r win-x64 --self-contained

# Ver base de datos
# macOS: ~/.local/share/HowsMyMoney/investments.db
# Windows: %LOCALAPPDATA%\HowsMyMoney\investments.db
```

---

## 📦 Dependencias

```xml
<PackageReference Include="Avalonia" Version="11.3.8" />
<PackageReference Include="Avalonia.Controls.DataGrid" Version="11.3.8" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="7.0.14" />
<PackageReference Include="LiveChartsCore.SkiaSharpView.Avalonia" Version="2.0.0-rc4.5" />
<PackageReference Include="CsvHelper" Version="30.0.1" />
```

---

## 🎯 APIs Utilizadas

### CoinGecko API (Criptomonedas)
- **Endpoint:** `https://api.coingecko.com/api/v3`
- **Gratuita:** Sí (50 req/min)
- **API Key:** No requerida
- **Docs:** https://www.coingecko.com/en/api

### CSGOBackpack API (Fallback para Skins)
- **Endpoint:** `https://csgobackpack.net/api`
- **Gratuita:** Sí
- **Solo CS:GO:** Sí (App ID: 730)

### Base de Datos Local (Primaria para Skins)
- **60+ skins populares de CS:GO**
- **Sin límites de tasa**
- **Offline-first**
- **Solo Counter-Strike: Global Offensive**

---

## 🌟 Características Destacadas

### ¿Por qué esta aplicación?
1. **Multiplataforma:** Funciona en macOS, Windows y Linux
2. **Sin conexión requerida (parcial):** Dashboard y gestión funcionan offline
3. **Privacidad:** Datos 100% locales, no se envían a ningún servidor
4. **Gratis:** No requiere API keys pagadas
5. **Open Source:** Código abierto para modificar según necesites
6. **CS:GO Específico:** Solo muestra items de Counter-Strike, no mezcla con otros juegos

### Flujo de Búsqueda CS:GO Optimizado
```
Usuario escribe "AK" → Busca en base de datos local →
Encuentra: AK-47 | Redline (FT) - $15.00
         AK-47 | Vulcan (FT) - $45.00
         AK-47 | Asiimov (FT) - $75.00
         ... (ordenados por precio MÁS BAJO)
Usuario selecciona → Precio se establece automáticamente
```

---

## 📝 Notas Técnicas

### Base de Datos SQLite
- **Auto-creación:** Se crea automáticamente al ejecutar
- **Migraciones:** Entity Framework Core maneja el esquema
- **Portable:** Copia el .db para backup

### MVVM Pattern
- **ViewModels:** Lógica de presentación
- **Commands:** IRelayCommand de CommunityToolkit
- **ObservableProperty:** Auto-generación de INotifyPropertyChanged
- **Data Binding:** Two-way binding con XAML

### LiveCharts2
- **LineChart:** Rendimiento mensual
- **PieChart:** Distribución de activos
- **SkiaSharp:** Renderizado de alto rendimiento

---

## 🐛 Debugging

Si tienes problemas:

1. **La app no inicia:**
   ```bash
   dotnet --version  # Debe ser 7.0+
   dotnet restore
   dotnet build
   ```

2. **No se actualizan los precios:**
   - Verifica conexión a internet
   - Revisa logs en consola (búsqueda "DEBUG:")
   - CoinGecko tiene límite de 50 req/min

3. **No aparecen skins:**
   - Asegúrate de seleccionar tipo "SkinCSGO"
   - Busca con 2+ caracteres
   - Revisa logs: "DEBUG CS:GO:"

4. **Base de datos corrupta:**
   ```bash
   # Eliminar y recrear
   rm ~/.local/share/HowsMyMoney/investments.db
   dotnet run  # Se creará nueva
   ```

---

## 🎉 ¡Proyecto Completado!

**Estado:** ✅ Producción  
**Versión:** 1.2  
**Plataformas:** macOS, Windows, Linux  
**Licencia:** MIT  

---

**Desarrollado con ❤️ usando .NET y Avalonia**
