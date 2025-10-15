# AI Interactive Surgery Simulator

Unity-based medical simulation showcasing two procedures—liver resection and soft-tissue stitching—guided by an AI virtual surgeon that provides real-time feedback for AIML learners [attached_file:1].

## Features
- Guided liver resection and stitching with step validation and scoring.
- AI surgeon with on-screen and voice feedback; multilingual guidance.
- Runs on desktop/mobile; basic hardware, no VR required.

## Quick Start
1. Open the Unity project and load MainMenu.unity.
2. Start the Python AI service (TensorFlow/PyTorch env) if using live feedback.
3. Choose a procedure and follow the on-screen/voice instructions.

## Controls
- Mouse/touch for tool use; on-screen tool wheel for scalpel, clamp, suction, needle driver, suture.
- Camera via mouse/keys or touch gestures, per scene settings.

## Output
- Post-session summary with timing, errors, adherence, and suggestions.