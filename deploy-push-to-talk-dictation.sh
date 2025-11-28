#!/bin/bash
set -e

# Push-to-Talk Dictation Service Deployment Script
# Follows C# .NET deployment workflow: test → build → deploy → restart

PROJECT_PATH="/home/jirka/Olbrasoft/VoiceAssistant"
DEPLOY_TARGET="/home/jirka/voice-assistant/push-to-talk-dictation"
SERVICE_NAME="push-to-talk-dictation.service"
SYSTEMD_DIR="$HOME/.config/systemd/user"

echo "========================================="
echo "Push-to-Talk Dictation - Deployment"
echo "========================================="

cd "$PROJECT_PATH"

# Step 1: Run tests
echo ""
echo "📋 Step 1/5: Running tests..."
dotnet test
if [ $? -ne 0 ]; then
    echo "❌ Tests failed! Aborting deployment."
    exit 1
fi
echo "✅ All tests passed!"

# Step 2: Build and deploy
echo ""
echo "📦 Step 2/5: Building and deploying..."
dotnet publish src/PushToTalkDictation.Service/PushToTalkDictation.Service.csproj \
  -c Release \
  -o "$DEPLOY_TARGET" \
  --no-self-contained

echo "✅ Build completed!"

# Step 3: Install systemd service
echo ""
echo "⚙️  Step 3/5: Installing systemd service..."
mkdir -p "$SYSTEMD_DIR"
cp systemd/push-to-talk-dictation.service "$SYSTEMD_DIR/"
systemctl --user daemon-reload
echo "✅ Service installed!"

# Step 4: Check user groups
echo ""
echo "🔐 Step 4/5: Checking permissions..."
if groups | grep -q '\binput\b'; then
    echo "✅ User is in 'input' group"
else
    echo "⚠️  WARNING: User is NOT in 'input' group!"
    echo "   Run: sudo usermod -a -G input $USER"
    echo "   Then logout and login again"
fi

# Step 5: Restart service
echo ""
echo "🔄 Step 5/5: Restarting service..."
systemctl --user restart "$SERVICE_NAME" 2>/dev/null || {
    echo "⚠️  Service not running yet, starting..."
    systemctl --user start "$SERVICE_NAME"
}

# Wait a moment for service to start
sleep 2

# Verify
echo ""
echo "📊 Service status:"
systemctl --user status "$SERVICE_NAME" --no-pager || true

echo ""
echo "========================================="
echo "✅ Deployment completed successfully!"
echo "========================================="
echo ""
echo "📝 Next steps:"
echo "  1. Check logs: journalctl --user -u $SERVICE_NAME -f"
echo "  2. Enable on boot: systemctl --user enable $SERVICE_NAME"
echo "  3. Test: Press CapsLock to start dictation"
echo ""
