#!/bin/bash

# Orchestration Service - Automated Deployment Script
# Deploys from: ~/Olbrasoft/VoiceAssistant/src/Orchestration
# Deploys to: ~/voice-assistant/orchestration
# Service: orchestration.service (systemd)
# This script builds, deploys, and verifies the Orchestration Service

set -e  # Exit on any error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$HOME/Olbrasoft/VoiceAssistant"
SERVICE_PROJECT="$PROJECT_ROOT/src/Orchestration/Orchestration.csproj"
DEPLOY_TARGET="$HOME/voice-assistant/orchestration"
SERVICE_NAME="orchestration.service"

echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}Orchestration Service Deployment${NC}"
echo -e "${GREEN}=====================================${NC}"
echo ""

# Step 1: Build release
echo -e "${YELLOW}Step 1: Building release...${NC}"
dotnet publish "$SERVICE_PROJECT" \
    -c Release \
    -o "$DEPLOY_TARGET" \
    --self-contained false \
    -r linux-x64 \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed! Deployment aborted.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Build completed${NC}"
echo ""

# Step 2: Restart service
echo -e "${YELLOW}Step 2: Restarting systemd service...${NC}"
systemctl --user restart "$SERVICE_NAME"
sleep 3

# Step 3: Verify service is running
echo -e "${YELLOW}Step 3: Verifying service status...${NC}"
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

# Step 4: Display service status
echo -e "${YELLOW}Step 4: Current service status:${NC}"
systemctl --user status "$SERVICE_NAME" --no-pager -l | head -15
echo ""

# Success summary
echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${GREEN}=====================================${NC}"
echo ""
echo "Management commands:"
echo "  - Status:  systemctl --user status $SERVICE_NAME"
echo "  - Stop:    systemctl --user stop $SERVICE_NAME"
echo "  - Start:   systemctl --user start $SERVICE_NAME"
echo "  - Restart: systemctl --user restart $SERVICE_NAME"
echo "  - Logs:    journalctl --user -u $SERVICE_NAME -f"
echo ""
