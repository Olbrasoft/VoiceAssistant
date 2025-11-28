#!/bin/bash
# Key Grabber Test Script
# Monitors /dev/input/event2 and shows key codes

echo "=== Key Code Grabber Test ==="
echo "Tento skript spustí key grabber na 30 sekund"
echo "STISKNĚTE RŮZNÉ KLÁVESY VČETNĚ CAPS LOCK!"
echo "Hledáme kód 58 (CapsLock) a 70 (ScrollLock)"
echo ""
echo "Spouští se..."
echo ""

cd "$(dirname "$0")"
timeout 30 sudo dotnet run

echo ""
echo "=== Test dokončen ===" echo "Pokud jste viděli kód 58 při stisku CAPS LOCK, funguje to!"
echo "Pokud ne, CAPS LOCK pravděpodobně používá jiný mechanismus (LED toggle)"
