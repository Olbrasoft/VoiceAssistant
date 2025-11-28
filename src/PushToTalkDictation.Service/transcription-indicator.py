#!/home/jirka/voice-assistant/push-to-talk-dictation/venv/bin/python3
"""
Transcription Indicator - System tray icon for speech-to-text status

Shows an animated document icon in the system tray when transcription is in progress.
Connects to PushToTalkDictation.Service via SignalR WebSocket.

Events:
- RecordingStopped (1): Show icon (transcription starting)
- TranscriptionCompleted (3): Hide icon
- TranscriptionFailed (4): Hide icon

Requires:
- pip install websocket-client requests
- libayatana-appindicator3-1 (for system tray)
"""

import gi

gi.require_version("AyatanaAppIndicator3", "0.1")
gi.require_version("Gtk", "3.0")
from gi.repository import AyatanaAppIndicator3 as AppIndicator3, Gtk, GLib

import signal
import sys
import os
import json
import threading
import time
from functools import partial
import websocket
import requests

# Unbuffered output for logging
print = partial(print, flush=True)

# Configuration
SIGNALR_URL = "http://localhost:5050/hubs/ptt"
WS_URL = "ws://localhost:5050/hubs/ptt"
ASSETS_DIR = "/home/jirka/voice-assistant/push-to-talk-dictation/assets"

# Event types from PttEventType enum
EVENT_RECORDING_STARTED = 0
EVENT_RECORDING_STOPPED = 1
EVENT_TRANSCRIPTION_STARTED = 2
EVENT_TRANSCRIPTION_COMPLETED = 3
EVENT_TRANSCRIPTION_FAILED = 4


class TranscriptionIndicator:
    """System tray indicator that shows during transcription."""

    def __init__(self):
        # Animation frames
        self.frames = [
            os.path.join(ASSETS_DIR, f"document-white-frame{i}.svg")
            for i in range(1, 6)
        ]
        self.current_frame = 0
        self.animation_timer = None
        self.ws = None
        self.running = True

        # Create AppIndicator (initially hidden)
        self.indicator = AppIndicator3.Indicator.new(
            "transcription-indicator",
            self.frames[0],
            AppIndicator3.IndicatorCategory.APPLICATION_STATUS,
        )

        # Start as PASSIVE (hidden)
        self.indicator.set_status(AppIndicator3.IndicatorStatus.PASSIVE)

        # Create menu (required by AppIndicator)
        menu = Gtk.Menu()

        status_item = Gtk.MenuItem(label="Transcription Indicator")
        status_item.set_sensitive(False)
        menu.append(status_item)
        status_item.show()

        separator = Gtk.SeparatorMenuItem()
        menu.append(separator)
        separator.show()

        quit_item = Gtk.MenuItem(label="Quit")
        quit_item.connect("activate", self.quit)
        menu.append(quit_item)
        quit_item.show()

        self.indicator.set_menu(menu)

        # Start WebSocket connection in background
        self.connect_websocket()

        print("✅ Transcription Indicator initialized")

    def connect_websocket(self):
        """Connect to SignalR hub via WebSocket."""

        def ws_thread():
            while self.running:
                try:
                    # Negotiate to get connection ID
                    print(f"🔌 Negotiating with {SIGNALR_URL}...")
                    resp = requests.post(f"{SIGNALR_URL}/negotiate", timeout=5)
                    data = resp.json()
                    conn_id = data["connectionId"]
                    print(f"📡 Got connection ID: {conn_id}")

                    # Connect WebSocket
                    ws_url = f"{WS_URL}?id={conn_id}"
                    print(f"🔌 Connecting to WebSocket...")

                    self.ws = websocket.WebSocketApp(
                        ws_url,
                        on_open=self.on_ws_open,
                        on_message=self.on_ws_message,
                        on_error=self.on_ws_error,
                        on_close=self.on_ws_close,
                    )
                    self.ws.run_forever()

                except Exception as e:
                    print(f"❌ Connection error: {e}")

                if self.running:
                    print("🔄 Reconnecting in 5 seconds...")
                    time.sleep(5)

        thread = threading.Thread(target=ws_thread, daemon=True)
        thread.start()

    def on_ws_open(self, ws):
        """Handle WebSocket open."""
        print("✅ WebSocket connected")
        # Send SignalR handshake
        handshake = json.dumps({"protocol": "json", "version": 1}) + "\x1e"
        ws.send(handshake)

    def on_ws_message(self, ws, message):
        """Handle WebSocket message."""
        # SignalR messages are separated by \x1e
        for msg in message.split("\x1e"):
            if not msg.strip():
                continue
            try:
                data = json.loads(msg)
                self.handle_signalr_message(data)
            except json.JSONDecodeError:
                pass

    def on_ws_error(self, ws, error):
        """Handle WebSocket error."""
        print(f"⚠️ WebSocket error: {error}")

    def on_ws_close(self, ws, close_status_code, close_msg):
        """Handle WebSocket close."""
        print(f"❌ WebSocket closed: {close_status_code} {close_msg}")

    def handle_signalr_message(self, data):
        """Handle parsed SignalR message."""
        msg_type = data.get("type")
        target = data.get("target")

        # Type 1 = Invocation (method call)
        if msg_type == 1 and target == "PttEvent":
            args = data.get("arguments", [])
            if args:
                self.on_ptt_event(args[0])

        # Type 6 = Ping - ignore
        elif msg_type == 6:
            pass

    def on_ptt_event(self, event):
        """Handle PTT events from SignalR."""
        # Event can be a list (from arguments) or dict
        if isinstance(event, list) and len(event) > 0:
            event = event[0]

        print(f"📨 Received event: {event}")

        event_type = event.get("eventType", -1)

        if event_type == EVENT_RECORDING_STOPPED:
            print("⏳ Recording stopped - showing indicator")
            GLib.idle_add(self.show_indicator)

        elif event_type in (EVENT_TRANSCRIPTION_COMPLETED, EVENT_TRANSCRIPTION_FAILED):
            text = event.get("text", "")
            error = event.get("errorMessage", "")
            if event_type == EVENT_TRANSCRIPTION_COMPLETED:
                print(f"✅ Transcription completed: {text[:50] if text else ''}...")
            else:
                print(f"❌ Transcription failed: {error}")
            GLib.idle_add(self.hide_indicator)

    def show_indicator(self):
        """Show the indicator with animation."""
        self.indicator.set_status(AppIndicator3.IndicatorStatus.ACTIVE)

        # Start animation if not already running
        if self.animation_timer is None:
            self.current_frame = 0
            self.animation_timer = GLib.timeout_add(200, self.animate)

        return False  # For GLib.idle_add

    def hide_indicator(self):
        """Hide the indicator and stop animation."""
        self.indicator.set_status(AppIndicator3.IndicatorStatus.PASSIVE)

        # Stop animation
        if self.animation_timer is not None:
            GLib.source_remove(self.animation_timer)
            self.animation_timer = None

        return False  # For GLib.idle_add

    def animate(self):
        """Cycle through animation frames."""
        self.current_frame = (self.current_frame + 1) % len(self.frames)
        self.indicator.set_icon_full(self.frames[self.current_frame], "Transcribing...")
        return True  # Continue animation

    def quit(self, widget=None):
        """Clean shutdown."""
        print("👋 Shutting down...")
        self.running = False

        if self.ws:
            try:
                self.ws.close()
            except:
                pass

        Gtk.main_quit()


def main():
    """Main entry point."""
    print("🎙️ Transcription Indicator starting...")
    print(f"   SignalR URL: {SIGNALR_URL}")
    print(f"   Assets: {ASSETS_DIR}")

    # Create indicator
    indicator = TranscriptionIndicator()

    # Handle Ctrl+C gracefully
    signal.signal(signal.SIGINT, lambda s, f: indicator.quit())
    signal.signal(signal.SIGTERM, lambda s, f: indicator.quit())

    # Run GTK main loop
    Gtk.main()


if __name__ == "__main__":
    main()
