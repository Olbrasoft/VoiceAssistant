# VoiceAssistant.Shared

Shared library containing common components for the Voice Assistant system.

## Components

### Speech Recognition (`Speech/`)
- **ISpeechTranscriber** - Interface for speech-to-text transcription
- **WhisperNetTranscriber** - Whisper.net implementation with CUDA GPU acceleration
- **OnnxWhisperTranscriber** - ONNX Runtime implementation for Whisper models
- **AudioPreprocessor** - Audio normalization and preprocessing
- **TokenDecoder** - Whisper token decoding utilities

### Text Input (`TextInput/`)
- **ITextTyper** - Interface for typing text into applications
- **DotoolTextTyper** - Linux Wayland implementation using dotool (recommended)
- **XdotoolTextTyper** - X11 implementation using xdotool
- **WtypeTextTyper** - Wayland implementation using wtype
- **TextTyperFactory** - Factory for creating appropriate text typer

### Input Utilities (`Input/`)
- **CapsLockState** - Caps Lock state detection

## Dependencies

- **Whisper.net** - Speech recognition with CUDA 13 GPU acceleration
- **NAudio** - Audio processing
- **Microsoft.ML.OnnxRuntime.Gpu** - ONNX inference with GPU support

## Usage

This library is referenced by:
- PushToTalkDictation
- Orchestration (planned)
