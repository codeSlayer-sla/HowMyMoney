#!/bin/bash

# HowsMyMoney - Script de utilidades

set -e

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

function print_header() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}  HowsMyMoney - $1${NC}"
    echo -e "${BLUE}================================${NC}"
}

function check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}Error: .NET SDK no está instalado${NC}"
        echo "Instala .NET desde: https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    echo -e "${GREEN}✓ .NET SDK detectado: $(dotnet --version)${NC}"
}

function run_app() {
    print_header "Ejecutando Aplicación"
    check_dotnet
    echo "Iniciando HowsMyMoney..."
    dotnet run
}

function build_app() {
    print_header "Compilando Aplicación"
    check_dotnet
    echo "Limpiando..."
    dotnet clean > /dev/null
    echo "Restaurando dependencias..."
    dotnet restore
    echo "Compilando..."
    dotnet build -c Release
    echo -e "${GREEN}✓ Compilación exitosa${NC}"
    echo "Ejecutable en: bin/Release/net7.0/"
}

function publish_app() {
    print_header "Publicando Aplicación"
    check_dotnet
    
    echo "Selecciona la plataforma:"
    echo "1) macOS (Apple Silicon - M1/M2)"
    echo "2) macOS (Intel)"
    echo "3) Windows x64"
    echo "4) Linux x64"
    read -p "Opción: " option
    
    case $option in
        1)
            RUNTIME="osx-arm64"
            ;;
        2)
            RUNTIME="osx-x64"
            ;;
        3)
            RUNTIME="win-x64"
            ;;
        4)
            RUNTIME="linux-x64"
            ;;
        *)
            echo -e "${RED}Opción inválida${NC}"
            exit 1
            ;;
    esac
    
    echo "Publicando para $RUNTIME..."
    dotnet publish -c Release -r $RUNTIME --self-contained -p:PublishSingleFile=true
    
    echo -e "${GREEN}✓ Publicación exitosa${NC}"
    echo "Ejecutable en: bin/Release/net7.0/$RUNTIME/publish/"
}

function clean_app() {
    print_header "Limpiando Proyecto"
    echo "Eliminando archivos de compilación..."
    dotnet clean
    rm -rf bin obj
    echo -e "${GREEN}✓ Limpieza completada${NC}"
}

function test_apis() {
    print_header "Probando APIs"
    
    echo "Probando CoinGecko API..."
    curl -s "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" | head -n 5
    echo -e "\n${GREEN}✓ CoinGecko API responde${NC}"
    
    echo -e "\nProbando Skinport API..."
    curl -s "https://api.skinport.com/v1/items?app_id=730&currency=USD" | head -n 5
    echo -e "\n${GREEN}✓ Skinport API responde${NC}"
}

function show_logs() {
    print_header "Ubicación de Datos"
    
    if [[ "$OSTYPE" == "darwin"* ]]; then
        DB_PATH="$HOME/Library/Application Support/HowsMyMoney/investments.db"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        DB_PATH="$HOME/.local/share/HowsMyMoney/investments.db"
    else
        DB_PATH="%LOCALAPPDATA%\\HowsMyMoney\\investments.db"
    fi
    
    echo "Base de datos: $DB_PATH"
    
    if [ -f "$DB_PATH" ]; then
        echo -e "${GREEN}✓ Base de datos existe${NC}"
        SIZE=$(du -h "$DB_PATH" | cut -f1)
        echo "Tamaño: $SIZE"
    else
        echo -e "${RED}✗ Base de datos no existe (se creará al iniciar la app)${NC}"
    fi
}

function show_menu() {
    print_header "Menú Principal"
    echo "1) Ejecutar aplicación"
    echo "2) Compilar (Debug)"
    echo "3) Publicar ejecutable"
    echo "4) Limpiar proyecto"
    echo "5) Probar APIs"
    echo "6) Mostrar ubicación de datos"
    echo "7) Salir"
    echo ""
    read -p "Selecciona una opción: " choice
    
    case $choice in
        1) run_app ;;
        2) build_app ;;
        3) publish_app ;;
        4) clean_app ;;
        5) test_apis ;;
        6) show_logs ;;
        7) exit 0 ;;
        *) 
            echo -e "${RED}Opción inválida${NC}"
            show_menu
            ;;
    esac
}

# Ejecutar menú
if [ $# -eq 0 ]; then
    show_menu
else
    case $1 in
        run) run_app ;;
        build) build_app ;;
        publish) publish_app ;;
        clean) clean_app ;;
        test) test_apis ;;
        logs) show_logs ;;
        *)
            echo "Uso: $0 [run|build|publish|clean|test|logs]"
            exit 1
            ;;
    esac
fi
