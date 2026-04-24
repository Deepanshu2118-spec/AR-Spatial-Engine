# AR Spatial Matrix & Assembly Engine

A real-time, markerless Augmented Reality workspace engineered for spatial CAD manipulation and mechanical assembly. 

This system bridges a computer vision backend with a 3D physics engine, allowing users to physically translate, assemble, scale, and tumble digital components using bare hands. It bypasses the need for proprietary VR/AR headsets, relying entirely on standard optical hardware and optimized mathematics.

## System Architecture

The pipeline is split into three core matrices:

1. **Vision Engine (Python):** - Utilizes `OpenCV` and `MediaPipe` for multi-vector skeletal tracking. 
   - Runs on the DirectShow (`CAP_DSHOW`) hardware API to bypass Windows OS throttling, ensuring maximum frame rate during parallel rendering.
   - Calculates Euclidean distance between digits to establish a binary "pinch" state.

2. **The Telemetry Bridge (UDP Socket):**
   - A custom low-latency local network bridge (Port 5052).
   - Packages dual-hand spatial coordinates and gesture states into a dynamic JSON array.
   - Flushes stale buffer queues instantly to prevent pipeline desync.

3. **Physics & State Machine (Unity C#):**
   - Parses the JSON matrix and maps normalized coordinates to 3D space.
   - **State A (Assembly):** Executes proximity-based "magnetic snapping" to lock independent parts into a parent-child hierarchy.
   - **State B (Manipulation):** When an assembled structure is grabbed by both hands, the engine locks translation and enables multi-vector Euclidean scaling, Z-axis rolling, and X/Y-axis tumbling.

## Core Files Provided

* `core_vision.py`: The headless hardware-accelerated computer vision tracker.
* `ARSystemManager.cs`: The dynamic state machine governing Unity physics and assembly logic.
* `UDPReceive.cs`: The optimized network parser and buffer-flush engine.

## Author
**Deepanshu Kumar** B.Tech Computer Science Engineering
