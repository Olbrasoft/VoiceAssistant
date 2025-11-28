#!/bin/bash
set -e

# Push-to-Talk Dictation Service Deployment Script
# Follows C# .NET deployment workflow: test → build → deploy → restart

PROJECT_PATH="/home/jirka/Olbrasoft/VoiceAssistant"
DEPLOY_TARGET="/home/jirka/voice-assistant/push-to-talk-dictation"
SERVICE_NAME="push-to-talk-dictation.service"
INDICATOR_SERVICE="transcription-indicator.service"
SYSTEMD_DIR="$HOME/.config/systemd/user"

echo "========================================="
echo "Push-to-Talk Dictation - Deployment"
echo "========================================="

cd "$PROJECT_PATH"

# Step 1: Run tests
echo ""
echo "📋 Step 1/6: Running tests..."
dotnet test
if [ $? -ne 0 ]; then
    echo "❌ Tests failed! Aborting deployment."
    exit 1
fi
echo "✅ All tests passed!"

# Step 2: Build and deploy
echo ""
echo "📦 Step 2/6: Building and deploying..."
dotnet publish src/PushToTalkDictation.Service/PushToTalkDictation.Service.csproj \
  -c Release \
  -o "$DEPLOY_TARGET" \
  --no-self-contained

# Copy assets and transcription indicator
mkdir -p "$DEPLOY_TARGET/assets"
cp src/PushToTalkDictation.Service/assets/*.svg "$DEPLOY_TARGET/assets/"
cp src/PushToTalkDictation.Service/transcription-indicator.py "$DEPLOY_TARGET/"
chmod +x "$DEPLOY_TARGET/transcription-indicator.py"

echo "✅ Build completed!"

# Step 3: Setup Python venv for transcription indicator
echo ""
echo "🐍 Step 3/6: Setting up Python environment..."
if [ ! -d "$DEPLOY_TARGET/venv" ]; then
    /usr/bin/python3 -m venv --system-site-packages "$DEPLOY_TARGET/venv"
    "$DEPLOY_TARGET/venv/bin/pip" install signalrcore
fi
echo "✅ Python environment ready!"

# Step 4: Install systemd services
echo ""
echo "⚙️  Step 4/6: Installing systemd services..."
mkdir -p "$SYSTEMD_DIR"
cp systemd/push-to-talk-dictation.service "$SYSTEMD_DIR/"
cp systemd/transcription-indicator.service "$SYSTEMD_DIR/"
systemctl --user daemon-reload
echo "✅ Services installed!"

# Step 5: Check user groups
echo ""
echo "🔐 Step 5/6: Checking permissions..."
if groups | grep -q '\binput\b'; then
    echo "✅ User is in 'input' group"
else
    echo "⚠️  WARNING: User is NOT in 'input' group!"
    echo "   Run: sudo usermod -a -G input $USER"
    echo "   Then logout and login again"
fi

# Step 6: Restart services
echo ""
echo "🔄 Step 6/6: Restarting services..."
systemctl --user restart "$SERVICE_NAME" 2>/dev/null || {
    echo "⚠️  Main service not running yet, starting..."
    systemctl --user start "$SERVICE_NAME"
}

systemctl --user restart "$INDICATOR_SERVICE" 2>/dev/null || {
    echo "⚠️  Indicator service not running yet, starting..."
    systemctl --user start "$INDICATOR_SERVICE"
}

# Wait a moment for services to start
sleep 2

# Verify
echo ""
echo "📊 Service status:"
systemctl --user status "$SERVICE_NAME" --no-pager || true
echo ""
systemctl --user status "$INDICATOR_SERVICE" --no-pager || true

echo ""
echo "========================================="
echo "✅ Deployment completed successfully!"
echo "========================================="
echo ""
echo "📝 Next steps:"
echo "  1. Check logs: journalctl --user -u $SERVICE_NAME -f"
echo "  2. Enable on boot: systemctl --user enable $SERVICE_NAME $INDICATOR_SERVICE"
echo "  3. Test: Press CapsLock to start dictation"
echo ""
