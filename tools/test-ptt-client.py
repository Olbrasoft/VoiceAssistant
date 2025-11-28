#!/usr/bin/env python3
"""
Simple SignalR client for testing PushToTalkDictation.Service notifications.
Requires: pip install signalrcore

Usage:
    python3 test-ptt-client.py
"""

import sys
import signal
from signalrcore.hub_connection_builder import HubConnectionBuilder

# SignalR hub URL
HUB_URL = "http://localhost:5050/hubs/ptt"


def on_connected(connection_id):
    """Handle connection event."""
    print(f"✅ Connected to PTT hub (ID: {connection_id})")


def on_subscribed(client_name):
    """Handle subscription confirmation."""
    print(f"📋 Subscribed as: {client_name}")


def on_ptt_event(event):
    """Handle PTT events from the service."""
    event_type = event.get("eventType", "Unknown")
    timestamp = event.get("timestamp", "")

    # Map event type numbers to names
    event_names = {
        0: "🎙️ RecordingStarted",
        1: "⏹️ RecordingStopped",
        2: "⏳ TranscriptionStarted",
        3: "✅ TranscriptionCompleted",
        4: "❌ TranscriptionFailed",
    }

    event_name = event_names.get(event_type, f"Unknown ({event_type})")

    print(f"\n{'=' * 50}")
    print(f"Event: {event_name}")
    print(f"Time:  {timestamp}")

    if event_type == 1:  # RecordingStopped
        duration = event.get("durationSeconds", 0)
        print(f"Duration: {duration:.2f}s")

    if event_type == 3:  # TranscriptionCompleted
        text = event.get("text", "")
        confidence = event.get("confidence", 0)
        print(f"Text: {text}")
        print(f"Confidence: {confidence:.3f}")

    if event_type == 4:  # TranscriptionFailed
        error = event.get("errorMessage", "Unknown error")
        print(f"Error: {error}")

    print("=" * 50)


def main():
    """Main entry point."""
    print(f"🔌 Connecting to {HUB_URL}...")

    hub_connection = (
        HubConnectionBuilder()
        .with_url(HUB_URL)
        .with_automatic_reconnect(
            {
                "type": "raw",
                "keep_alive_interval": 10,
                "reconnect_interval": 5,
                "max_attempts": 5,
            }
        )
        .build()
    )

    # Register event handlers
    hub_connection.on("Connected", on_connected)
    hub_connection.on("Subscribed", on_subscribed)
    hub_connection.on("PttEvent", on_ptt_event)

    # Handle Ctrl+C gracefully
    def signal_handler(sig, frame):
        print("\n👋 Disconnecting...")
        hub_connection.stop()
        sys.exit(0)

    signal.signal(signal.SIGINT, signal_handler)

    # Start connection
    hub_connection.start()

    # Subscribe with a client name
    hub_connection.send("Subscribe", ["TestClient"])

    print("📡 Waiting for PTT events... (Press Ctrl+C to exit)")

    # Keep the main thread alive
    try:
        while True:
            signal.pause()
    except KeyboardInterrupt:
        hub_connection.stop()


if __name__ == "__main__":
    main()
