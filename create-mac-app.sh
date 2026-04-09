#!/bin/bash

echo "🚀 Creando aplicación macOS con icono personalizado..."

APP_NAME="HowsMyMoney"
BUNDLE_ID="com.howsmymoney.app"
VERSION="1.0.0"
PUBLISH_DIR="./publish"
APP_DIR="./${APP_NAME}.app"

# 1. Limpiar publicaciones anteriores
echo "🧹 Limpiando builds anteriores..."
rm -rf "$PUBLISH_DIR"
rm -rf "$APP_DIR"

# 2. Publicar la aplicación
echo "📦 Publicando aplicación..."
dotnet publish -c Release -r osx-arm64 --self-contained false -o "$PUBLISH_DIR"

if [ $? -ne 0 ]; then
    echo "❌ Error al publicar la aplicación"
    exit 1
fi

# 3. Crear estructura del bundle .app
echo "📁 Creando bundle de macOS..."
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# 4. Copiar el ejecutable
echo "📋 Copiando ejecutable..."
cp -r "$PUBLISH_DIR"/* "$APP_DIR/Contents/MacOS/"

# 5. Copiar el icono
echo "🎨 Copiando icono..."
cp Assets/AppIcon.icns "$APP_DIR/Contents/Resources/"

# 6. Crear Info.plist
echo "📝 Creando Info.plist..."
cat > "$APP_DIR/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon.icns</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
EOF

# 7. Hacer ejecutable el archivo principal
echo "🔧 Configurando permisos..."
chmod +x "$APP_DIR/Contents/MacOS/$APP_NAME"

# 8. Limpiar directorio de publicación temporal
rm -rf "$PUBLISH_DIR"

echo "✅ ¡Aplicación creada exitosamente!"
echo ""
echo "📍 Ubicación: $(pwd)/$APP_DIR"
echo ""
echo "Para ejecutar la aplicación:"
echo "   open $APP_DIR"
echo ""
echo "Para instalar en Aplicaciones:"
echo "   cp -r $APP_DIR /Applications/"
echo ""
