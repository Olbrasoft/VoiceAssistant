#!/home/jirka/voice-assistant/push-to-talk-dictation/venv/bin/python3
"""
Transcription Indicator - System tray icon for speech-to-text status

Shows an animated document icon in the system tray when transcription is in progress.
Connects to PushToTalkDictation.Service via SignalR to receive real-time events.

Events:
- TranscriptionStarted (2): Show icon with animation
- TranscriptionCompleted (3): Hide icon
- TranscriptionFailed (4): Hide icon

Requires:
- pip install signalrcore
- libayatana-appindicator3-1 (for system tray)
"""

import gi

gi.require_version("AyatanaAppIndicator3", "0.1")
gi.require_version("Gtk", "3.0")
from gi.repository import AyatanaAppIndicator3 as AppIndicator3, Gtk, GLib

import signal
import sys
import os
import threading
from functools import partial
from signalrcore.hub_connection_builder import HubConnectionBuilder

# Unbuffered output for logging
print = partial(print, flush=True)

# Configuration
SIGNALR_URL = "http://localhost:5050/hubs/ptt"
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

        # SignalR connection
        self.hub_connection = None
        self.connect_signalr()

        print("✅ Transcription Indicator initialized")

    def connect_signalr(self):
        """Connect to SignalR hub."""
        print(f"🔌 Connecting to {SIGNALR_URL}...")

        self.hub_connection = (
            HubConnectionBuilder()
            .with_url(SIGNALR_URL)
            .with_automatic_reconnect(
                {
                    "type": "raw",
                    "keep_alive_interval": 10,
                    "reconnect_interval": 5,
                    "max_attempts": 100,
                }
            )
            .build()
        )

        # Register event handlers
        self.hub_connection.on("Connected", self.on_connected)
        self.hub_connection.on("PttEvent", self.on_ptt_event)
        self.hub_connection.on_open(lambda: print("✅ SignalR connected"))
        self.hub_connection.on_close(lambda: print("❌ SignalR disconnected"))
        self.hub_connection.on_error(lambda e: print(f"⚠️ SignalR error: {e}"))

        # Start connection in background thread
        def start_connection():
            try:
                self.hub_connection.start()
            except Exception as e:
                print(f"❌ Failed to connect: {e}")
                # Retry after 5 seconds
                GLib.timeout_add_seconds(5, self.retry_connect)

        thread = threading.Thread(target=start_connection, daemon=True)
        thread.start()

    def retry_connect(self):
        """Retry SignalR connection."""
        print("🔄 Retrying SignalR connection...")
        self.connect_signalr()
        return False  # Don't repeat

    def on_connected(self, connection_id):
        """Handle connection event."""
        print(f"📡 Connected with ID: {connection_id}")

    def on_ptt_event(self, event):
        """Handle PTT events from SignalR."""
        event_type = event.get("eventType", -1)

        if event_type == EVENT_TRANSCRIPTION_STARTED:
            print("⏳ Transcription started - showing indicator")
            GLib.idle_add(self.show_indicator)

        elif event_type in (EVENT_TRANSCRIPTION_COMPLETED, EVENT_TRANSCRIPTION_FAILED):
            text = event.get("text", "")
            error = event.get("errorMessage", "")
            if event_type == EVENT_TRANSCRIPTION_COMPLETED:
                print(f"✅ Transcription completed: {text[:50]}...")
            else:
                print(f"❌ Transcription failed: {error}")
            GLib.idle_add(self.hide_indicator)

    def show_indicator(self):
        """Show the indicator with animation."""
        self.indicator.set_status(AppIndicator3.IndicatorStatus.ACTIVE)

        # Start animation if not already running
        if self.animation_timer is None:
            self.animation_timer = GLib.timeout_add(400, self.animate)

        return False  # For GLib.idle_add

    def hide_indicator(self):
        """Hide the indicator."""
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

        if self.hub_connection:
            try:
                self.hub_connection.stop()
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
