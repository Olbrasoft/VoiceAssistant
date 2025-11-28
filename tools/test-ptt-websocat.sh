#!/bin/bash
# Simple test client for PTT SignalR hub using websocat
# Install: cargo install websocat OR apt install websocat
#
# Usage:
#   ./test-ptt-websocat.sh
#
# Note: SignalR uses a specific protocol. This script shows raw messages.
# For full SignalR support, use the Python client (test-ptt-client.py).

HUB_URL="ws://localhost:5050/hubs/ptt"

echo "🔌 Connecting to $HUB_URL..."
echo "   (Press Ctrl+C to exit)"
echo ""

# SignalR requires a negotiate step first
NEGOTIATE=$(curl -s "http://localhost:5050/hubs/ptt/negotiate?negotiateVersion=1" \
    -H "Content-Type: application/json" \
    -X POST)

if [ $? -ne 0 ]; then
    echo "❌ Failed to negotiate with SignalR hub"
    echo "   Make sure PushToTalkDictation.Service is running on port 5050"
    exit 1
fi

echo "📋 Negotiate response:"
echo "$NEGOTIATE" | jq .

CONNECTION_TOKEN=$(echo "$NEGOTIATE" | jq -r '.connectionToken // .connectionId')

if [ "$CONNECTION_TOKEN" == "null" ] || [ -z "$CONNECTION_TOKEN" ]; then
    echo "❌ Could not get connection token"
    exit 1
fi

echo ""
echo "📡 Connecting with token: $CONNECTION_TOKEN"
echo ""

# Connect to WebSocket with the connection token
websocat "${HUB_URL}?id=${CONNECTION_TOKEN}" -v
