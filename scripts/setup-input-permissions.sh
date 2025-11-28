#!/bin/bash
# Setup input device permissions for keyboard monitoring

echo "========================================="
echo "Input Permissions Setup"
echo "========================================="
echo ""

# Check if already in input group
if groups | grep -q '\binput\b'; then
    echo "✅ User '$USER' is already in 'input' group!"
    echo ""
    echo "You can access /dev/input/eventX devices."
    exit 0
fi

echo "Adding user '$USER' to 'input' group..."
echo ""

sudo usermod -a -G input $USER

if [ $? -eq 0 ]; then
    echo "✅ Successfully added to 'input' group!"
    echo ""
    echo "⚠️  IMPORTANT: You must LOGOUT and LOGIN again for changes to take effect!"
    echo ""
    echo "After logout/login, verify with:"
    echo "  groups | grep input"
    echo ""
else
    echo "❌ Failed to add user to 'input' group"
    echo "   You may need sudo permissions"
    exit 1
fi
