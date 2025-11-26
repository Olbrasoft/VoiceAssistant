#!/usr/bin/env python3
"""
Simple audio transcription using faster-whisper
Called by VoiceAssistant C# service
Usage: ./transcribe-audio.py <audio_file.wav>
Output: JSON with transcribed text
"""

import sys
import os
import json

# Setup CUDNN library path for GPU support BEFORE importing WhisperModel
cudnn_path = os.path.expanduser(
    "~/.local/lib/python3.13/site-packages/nvidia/cudnn/lib"
)
cublas_path = os.path.expanduser(
    "~/.local/lib/python3.13/site-packages/nvidia/cublas/lib"
)
cuda_runtime_path = os.path.expanduser(
    "~/.local/lib/python3.13/site-packages/nvidia/cuda_runtime/lib"
)
if os.path.exists(cudnn_path):
    os.environ["LD_LIBRARY_PATH"] = (
        f"{cudnn_path}:{cublas_path}:{cuda_runtime_path}:"
        + os.environ.get("LD_LIBRARY_PATH", "")
    )

try:
    from faster_whisper import WhisperModel
except ImportError:
    print(json.dumps({"error": "faster-whisper not installed"}))
    sys.exit(1)

# Configuration
WHISPER_MODEL_SIZE = "medium"  # medium model for better accuracy
WHISPER_LANGUAGE = "cs"  # Czech

# Global model instance (cached)
whisper_model = None


def load_whisper_model():
    """Load Whisper model (lazy loading)."""
    global whisper_model
    if whisper_model is None:
        try:
            # Try GPU first
            whisper_model = WhisperModel(
                WHISPER_MODEL_SIZE, device="cuda", compute_type="float16"
            )
        except Exception as e:
            # Fallback to CPU
            whisper_model = WhisperModel(
                WHISPER_MODEL_SIZE, device="cpu", compute_type="int8"
            )
    return whisper_model


def transcribe_audio(audio_file):
    """Transcribe audio file with Whisper."""
    try:
        model = load_whisper_model()

        # Transcribe with advanced settings
        segments, info = model.transcribe(
            audio_file,
            language=WHISPER_LANGUAGE,
            beam_size=5,
            word_timestamps=True,
            condition_on_previous_text=True,
            vad_filter=True,  # Voice Activity Detection
        )

        # Get text
        text = " ".join([segment.text for segment in segments]).strip()

        return {"text": text, "language": info.language, "duration": info.duration}

    except Exception as e:
        return {"error": str(e)}


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: transcribe-audio.py <audio_file.wav>"}))
        sys.exit(1)

    audio_file = sys.argv[1]

    if not os.path.exists(audio_file):
        print(json.dumps({"error": f"File not found: {audio_file}"}))
        sys.exit(1)

    result = transcribe_audio(audio_file)
    print(json.dumps(result))
