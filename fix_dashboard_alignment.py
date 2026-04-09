#!/usr/bin/env python3
import re

# Leer el archivo
with open('/Users/josesalcedo/Desktop/HowsMyMoney/Views/DashboardView.axaml', 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Cambiar ColumnDefinitions de Auto a anchos fijos
content = re.sub(
    r'<Grid ColumnDefinitions="Auto,Auto,Auto,Auto,Auto,Auto"( MinWidth="900"|)>',
    r'<Grid ColumnDefinitions="280,140,140,140,180,60" MinWidth="940">',
    content
)

# 2. Cambiar MinWidth="200" por nada en Grid Grid.Column="0"
content = re.sub(
    r'<Grid Grid\.Column="0" ColumnDefinitions="Auto,\*" Margin="0,0,30,0" MinWidth="200">',
    r'<Grid Grid.Column="0" ColumnDefinitions="Auto,*" Margin="0,0,20,0">',
    content
)

# 3. Eliminar MaxWidth="200" de StackPanel
content = re.sub(
    r'<StackPanel Grid\.Column="1" \n\s+VerticalAlignment="Center"\n\s+MaxWidth="200">',
    r'<StackPanel Grid.Column="1" \n                                                           VerticalAlignment="Center">',
    content
)

# 4. Cambiar márgenes de 30 a 15 en las columnas de datos
content = re.sub(
    r'Margin="0,0,30,0" VerticalAlignment="Center">',
    r'Margin="0,0,15,0" VerticalAlignment="Center">',
    content
)

# 5. Cambiar padding de Ganancia/Pérdida
content = re.sub(
    r'Padding="20,12"\n\s+Margin="0,0,20,0"',
    r'Padding="15,12"\n                                                   Margin="0,0,10,0"',
    content
)

# Guardar el archivo
with open('/Users/josesalcedo/Desktop/HowsMyMoney/Views/DashboardView.axaml', 'w', encoding='utf-8') as f:
    f.write(content)

print("✅ Cambios aplicados correctamente")
