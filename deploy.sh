#!/bin/bash

# WakeWord Listener - Automated Deployment Script
# This script builds, tests, deploys, and verifies the WakeWord Listener service

set -e  # Exit on any error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$HOME/Olbrasoft/VoiceAssistant"
SERVICE_PROJECT="$PROJECT_ROOT/src/WakeWordDetection.Service/WakeWordDetection.Service.csproj"
DEPLOY_TARGET="$HOME/voice-assistant/wakeword-listener"
SERVICE_NAME="wakeword-listener.service"

echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}WakeWord Listener Deployment${NC}"
echo -e "${GREEN}=====================================${NC}"
echo ""

# Step 1: Run tests
echo -e "${YELLOW}Step 1: Running tests...${NC}"
cd "$PROJECT_ROOT"
dotnet test --verbosity minimal

if [ $? -ne 0 ]; then
    echo -e "${RED}Tests failed! Deployment aborted.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ All tests passed${NC}"
echo ""

# Step 2: Build self-contained release
echo -e "${YELLOW}Step 2: Building self-contained release...${NC}"
dotnet publish "$SERVICE_PROJECT" \
    -c Release \
    -o "$DEPLOY_TARGET" \
    --self-contained true \
    -r linux-x64 \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed! Deployment aborted.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Build completed${NC}"
echo ""

# Step 3: Restart service
echo -e "${YELLOW}Step 3: Restarting systemd service...${NC}"
systemctl --user restart "$SERVICE_NAME"
sleep 3

# Step 4: Verify service is running
echo -e "${YELLOW}Step 4: Verifying service status...${NC}"
if systemctl --user is-active --quiet "$SERVICE_NAME"; then
    echo -e "${GREEN}✓ Service is running${NC}"
else
    echo -e "${RED}✗ Service failed to start!${NC}"
    echo ""
    echo "Recent logs:"
    journalctl --user -u "$SERVICE_NAME" -n 20 --no-pager
    exit 1
fi
echo ""

# Step 5: Test HTTP endpoint
echo -e "${YELLOW}Step 5: Testing HTTP endpoint...${NC}"
HTTP_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/api/wakeword/status)

if [ "$HTTP_RESPONSE" = "200" ]; then
    echo -e "${GREEN}✓ HTTP endpoint responding (Status: $HTTP_RESPONSE)${NC}"
    
    # Get and display service info
    SERVICE_INFO=$(curl -s http://localhost:5000/api/wakeword/info)
    echo ""
    echo "Service Info:"
    echo "$SERVICE_INFO" | python3 -m json.tool 2>/dev/null || echo "$SERVICE_INFO"
else
    echo -e "${RED}✗ HTTP endpoint not responding (Status: $HTTP_RESPONSE)${NC}"
    exit 1
fi
echo ""

# Step 6: Display service status
echo -e "${YELLOW}Step 6: Current service status:${NC}"
systemctl --user status "$SERVICE_NAME" --no-pager -l | head -15
echo ""

# Success summary
echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${GREEN}=====================================${NC}"
echo ""
echo "Service endpoints:"
echo "  - Status:  http://localhost:5000/api/wakeword/status"
echo "  - Info:    http://localhost:5000/api/wakeword/info"
echo "  - Words:   http://localhost:5000/api/wakeword/words"
echo "  - Trigger: POST http://localhost:5000/api/wakeword/trigger?word=jarvis"
echo "  - WebSocket: ws://localhost:5000/hubs/wakeword"
echo ""
echo "Management commands:"
echo "  - Status:  systemctl --user status $SERVICE_NAME"
echo "  - Stop:    systemctl --user stop $SERVICE_NAME"
echo "  - Start:   systemctl --user start $SERVICE_NAME"
echo "  - Restart: systemctl --user restart $SERVICE_NAME"
echo "  - Logs:    journalctl --user -u $SERVICE_NAME -f"
echo ""
