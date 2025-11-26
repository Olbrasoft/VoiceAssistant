#!/usr/bin/env python3
"""
Audio recording with silence detection for VoiceAssistant
Records audio from microphone, detects silence, and saves to WAV file
Usage: ./record-audio.py <output_file.wav> [max_duration_seconds] [silence_threshold] [max_silence_seconds]
"""

import sys
import pyaudio
import wave
import struct
import time

# Default parameters
DEFAULT_MAX_DURATION = 30  # seconds
DEFAULT_SILENCE_THRESHOLD = 800  # amplitude (0-32767)
DEFAULT_MAX_SILENCE = 3.0  # seconds


def record_audio_with_silence_detection(
    output_file,
    max_duration_seconds=DEFAULT_MAX_DURATION,
    silence_threshold=DEFAULT_SILENCE_THRESHOLD,
    max_silence_seconds=DEFAULT_MAX_SILENCE,
):
    """Record audio from microphone with silence detection."""

    # Audio configuration (optimized for Whisper)
    RATE = 16000  # 16kHz
    CHANNELS = 1  # Mono
    FORMAT = pyaudio.paInt16  # 16-bit PCM
    CHUNK = 1024  # Samples per buffer

    pa = pyaudio.PyAudio()

    try:
        # Open audio stream
        stream = pa.open(
            rate=RATE,
            channels=CHANNELS,
            format=FORMAT,
            input=True,
            frames_per_buffer=CHUNK,
        )

        print(f"🎤 Recording to {output_file}...", file=sys.stderr, flush=True)

        frames = []
        silence_chunks = 0
        max_silence_chunks = int(max_silence_seconds * RATE / CHUNK)
        max_total_chunks = int(max_duration_seconds * RATE / CHUNK)
        total_chunks = 0
        has_spoken = False  # Track if user has spoken at all
        waiting_for_speech_chunks = 0
        max_waiting_chunks = int(5.0 * RATE / CHUNK)  # 5 seconds to start speaking

        # Calibration phase
        calibration_samples = []
        is_calibrating = True
        calibration_chunks = 8

        while total_chunks < max_total_chunks:
            data = stream.read(CHUNK, exception_on_overflow=False)
            total_chunks += 1

            # Calibration: measure noise level
            if is_calibrating:
                # Skip first 3 chunks to let audio stabilize
                if total_chunks <= 3:
                    continue

                audio_data = struct.unpack(f"{CHUNK}h", data)
                amplitude = sum(abs(x) for x in audio_data) / len(audio_data)
                calibration_samples.append(amplitude)

                if len(calibration_samples) >= calibration_chunks - 3:
                    avg_noise = sum(calibration_samples) / len(calibration_samples)
                    adjusted_threshold = max(avg_noise * 2.0, silence_threshold)
                    silence_threshold = min(adjusted_threshold, 1200)

                    print(
                        f"📊 Calibrated silence threshold: {int(silence_threshold)}",
                        file=sys.stderr,
                        flush=True,
                    )
                    is_calibrating = False
                continue

            # Calculate current amplitude
            audio_data = struct.unpack(f"{CHUNK}h", data)
            current_amplitude = sum(abs(x) for x in audio_data) / len(audio_data)

            # Check if speaking (above threshold)
            if current_amplitude > silence_threshold:
                has_spoken = True
                silence_chunks = 0
                waiting_for_speech_chunks = 0
            else:
                silence_chunks += 1
                if not has_spoken:
                    waiting_for_speech_chunks += 1

            # Store audio data
            frames.append(data)

            # Timeout if user hasn't started speaking
            if not has_spoken and waiting_for_speech_chunks >= max_waiting_chunks:
                print(
                    f"⏱️  No speech detected within 5 seconds, stopping",
                    file=sys.stderr,
                    flush=True,
                )
                break

            # Stop if max silence reached AFTER user has spoken
            if has_spoken and silence_chunks >= max_silence_chunks:
                print(
                    f"✅ Silence detected, stopping recording",
                    file=sys.stderr,
                    flush=True,
                )
                break

        stream.stop_stream()
        stream.close()

        # Save to WAV file
        with wave.open(output_file, "wb") as wf:
            wf.setnchannels(CHANNELS)
            wf.setsampwidth(pa.get_sample_size(FORMAT))
            wf.setframerate(RATE)
            wf.writeframes(b"".join(frames))

        duration = len(frames) * CHUNK / RATE
        print(f"✅ Recorded {duration:.1f}s", file=sys.stderr, flush=True)

        return True

    except Exception as e:
        print(f"❌ Recording error: {e}", file=sys.stderr, flush=True)
        return False

    finally:
        pa.terminate()


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(
            "Usage: record-audio.py <output_file.wav> [max_duration] [silence_threshold] [max_silence]",
            file=sys.stderr,
        )
        sys.exit(1)

    output_file = sys.argv[1]
    max_duration = int(sys.argv[2]) if len(sys.argv) > 2 else DEFAULT_MAX_DURATION
    silence_threshold = (
        int(sys.argv[3]) if len(sys.argv) > 3 else DEFAULT_SILENCE_THRESHOLD
    )
    max_silence = float(sys.argv[4]) if len(sys.argv) > 4 else DEFAULT_MAX_SILENCE

    success = record_audio_with_silence_detection(
        output_file, max_duration, silence_threshold, max_silence
    )

    sys.exit(0 if success else 1)
