# 🚀 Guía de Inicio Rápido - HowsMyMoney

Esta guía te ayudará a poner en marcha la aplicación en menos de 5 minutos.

## ⚡ Inicio Rápido

### Paso 1: Verificar Requisitos

Abre una terminal y ejecuta:

```bash
dotnet --version
```

Deberías ver algo como `7.0.x` o superior. Si no:
- **macOS**: `brew install dotnet`
- **Windows**: Descarga desde https://dotnet.microsoft.com/download
- **Linux**: Sigue las instrucciones en https://learn.microsoft.com/dotnet/core/install/linux

### Paso 2: Ejecutar la Aplicación

```bash
cd /Users/josesalcedo/Desktop/HowsMyMoney
dotnet run
```

¡Eso es todo! La aplicación se abrirá automáticamente.

## 📝 Tu Primera Inversión

### Ejemplo 1: Agregar Bitcoin

1. Haz clic en **"➕ Nueva Inversión"**
2. Tipo de activo: `Criptomoneda`
3. Nombre: `Bitcoin` → Clic en **🔍 Buscar**
4. El sistema encontrará automáticamente:
   - Nombre: Bitcoin
   - Símbolo: bitcoin
   - Precio actual: $XX,XXX.XX
5. Completa:
   - Cantidad: `0.5` (medio bitcoin)
   - Precio de compra: `$30000` (lo que pagaste)
   - Fecha: Selecciona cuando compraste
6. **💾 Guardar Inversión**

### Ejemplo 2: Agregar una Skin de CS:GO

1. Haz clic en **"➕ Nueva Inversión"**
2. Tipo de activo: `SkinCSGO`
3. Nombre: `AK-47` → Clic en **🔍 Buscar**
4. Selecciona de los resultados (ej: AK-47 | Redline (Field-Tested))
5. El precio se actualizará automáticamente
6. Cantidad: `1`
7. Ajusta el precio de compra si es diferente
8. **💾 Guardar Inversión**

### Ejemplo 3: Agregar un Activo Manual

1. Haz clic en **"➕ Nueva Inversión"**
2. Tipo de activo: `Manual`
3. Nombre: `Reloj Rolex Submariner`
4. Cantidad: `1`
5. Precio de compra: `$8000`
6. Precio actual: `$12000` (valor estimado actual)
7. Notas: `Comprado en 2020, modelo 116610LN`
8. **💾 Guardar Inversión**

## 📊 Interpretar el Dashboard

### Tarjetas de Resumen

```
┌─────────────────────┬──────────────────┬──────────────────┬────────────────────┐
│ Total Inversiones   │ Total Invertido  │ Valor Actual     │ Ganancia/Pérdida   │
│        5            │    $50,000       │    $65,000       │  +$15,000 (30%)    │
└─────────────────────┴──────────────────┴──────────────────┴────────────────────┘
```

- **Total Inversiones**: Número de activos diferentes
- **Total Invertido**: Suma de lo que pagaste por todo
- **Valor Actual**: Cuánto valen tus inversiones ahora
- **Ganancia/Pérdida**: Diferencia entre valor actual e invertido

### Colores de Rentabilidad

- 🟢 **Verde**: Ganancia (valor subió)
- 🔴 **Rojo**: Pérdida (valor bajó)
- ⚪ **Gris**: Sin cambio

### Gráfico Mensual

Muestra cómo ha evolucionado el valor total de tu portfolio mes a mes. Útil para ver tendencias a largo plazo.

### Gráfico de Distribución

Muestra qué porcentaje de tu portfolio está en:
- 💎 Criptomonedas
- 🎮 Skins CS:GO
- 📝 Activos Manuales

## 🔄 Actualizar Precios

**Importante**: Actualiza los precios antes de tomar decisiones.

1. En el Dashboard, haz clic en **"🔄 Actualizar Precios"**
2. Espera unos segundos
3. Los precios de criptos y skins se sincronizarán automáticamente
4. Los activos manuales no cambian (actualízalos manualmente si quieres)

**Frecuencia recomendada**: 
- Día trading: Cada hora
- Inversión a largo plazo: Una vez al día o semana

## 💾 Exportar tus Datos

1. Haz clic en **"💾 Exportar CSV"**
2. El archivo se guarda en tu carpeta Documentos
3. Nombre: `HowsMyMoney_Export_20251107_143022.csv`
4. Abre con Excel, Google Sheets o Numbers

### Columnas del CSV

| Columna | Descripción |
|---------|-------------|
| Nombre | Nombre del activo |
| Tipo | Criptomoneda / SkinCSGO / Manual |
| Cantidad | Unidades que posees |
| Precio Compra | Lo que pagaste por unidad |
| Precio Actual | Valor actual por unidad |
| Valor Actual | Cantidad × Precio Actual |
| Ganancia/Pérdida | Diferencia en USD |
| Rentabilidad % | Porcentaje de ganancia/pérdida |

## ⚙️ Configuración Común

### Cambiar el idioma de números

Los números se muestran en formato USD por defecto. Para cambiar:
- Formato actual: `$1,234.56`
- El formato se basa en tu configuración regional del sistema

### Dónde están mis datos

Tu base de datos está en:

**macOS**:
```
/Users/TU_USUARIO/Library/Application Support/HowsMyMoney/investments.db
```

**Windows**:
```
C:\Users\TU_USUARIO\AppData\Local\HowsMyMoney\investments.db
```

**Linux**:
```
~/.local/share/HowsMyMoney/investments.db
```

### Hacer copia de seguridad

Simplemente copia el archivo `investments.db` a un lugar seguro (Google Drive, USB, etc.)

Para restaurar: Reemplaza el archivo con tu copia de seguridad.

## 🎯 Consejos Profesionales

### 1. Registra el precio de compra real
No uses el precio actual al agregar una inversión antigua. Anota el precio que pagaste realmente para ver ganancias/pérdidas reales.

### 2. Actualiza precios regularmente
Los precios de criptos cambian constantemente. Actualiza al menos una vez al día si haces seguimiento activo.

### 3. Usa notas
Las notas te ayudan a recordar por qué compraste algo:
- "Comprado después de la caída del mercado"
- "Recomendación de Juan"
- "HODL hasta 2030"

### 4. Revisa el rendimiento mensual
El gráfico mensual te ayuda a ver patrones. ¿Subes más en invierno? ¿Tus inversiones son muy volátiles?

### 5. Diversifica
El gráfico de distribución te muestra si estás muy concentrado en un tipo. Considera diversificar si una categoría es >70% de tu portfolio.

## ❓ Preguntas Frecuentes

**P: ¿Necesito internet para usar la app?**
R: Solo para actualizar precios de criptos y skins. Puedes ver tus datos sin conexión.

**P: ¿Mis datos son privados?**
R: Sí, todo se guarda localmente en tu computadora. No enviamos nada a servidores.

**P: ¿Puedo usar la app en varios dispositivos?**
R: Copia el archivo `investments.db` entre dispositivos. Próximamente: sincronización en la nube.

**P: ¿Qué pasa si borro una inversión?**
R: Se elimina permanentemente de la base de datos (pero puedes restaurar desde un backup).

**P: ¿Soporta múltiples monedas?**
R: Actualmente solo USD. Próximamente: EUR, MXN, ARS, etc.

**P: ¿Puedo ver inversiones de años anteriores?**
R: Sí, el historial de precios se guarda mensualmente desde que agregas la inversión.

## 🆘 Necesitas Ayuda?

Si algo no funciona:

1. **Reinicia la aplicación**: Cierra y vuelve a abrir
2. **Revisa el README.md**: Documentación completa
3. **Verifica tu conexión**: Necesaria para actualizar precios
4. **Actualiza .NET**: `dotnet --version` debe ser 7.0+

---

¡Disfruta gestionando tus inversiones! 🚀💰
