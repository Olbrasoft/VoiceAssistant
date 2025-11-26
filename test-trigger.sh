#!/bin/bash
echo "🗣️ Triggering wake word detection..."
curl -X POST "http://localhost:5000/api/wakeword/trigger?word=jarvis"
echo ""
