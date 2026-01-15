Axiom Vessel Diagnostics Suite
A modular, audit‑grade debugging framework for marine simulation in Unity.

🌊 Overview
The Axiom Vessel Diagnostics Suite provides a complete set of tools for inspecting, validating, and debugging the physical behaviour of marine vessels in Unity.
It focuses on Centre of Mass (COM) accuracy, roll behaviour, buoyancy distribution, and force application correctness — all essential for stable, physically authentic marine simulation.
This package is designed to be:
- Modular — each component has a single responsibility
- Audit‑driven — every diagnostic is explicit and traceable
- Non‑intrusive — tools never interfere with runtime physics
- Future‑proof — clean APIs for expansion and automation

📦 Included Components
### 1. BoatCOM
Role: COM authority for the vessel.
Responsibilities:
- Stores COM height relative to the vessel root
- Defines the neutral band (no‑roll zone)
- Applies COM offsets when enabled
- Performs COM and neutral band validation
- Acts as the single source of truth for all COM‑dependent systems
This component is foundational for all roll and buoyancy diagnostics.

2. BoatCOMIntegration
Role: Bridge between COM data and external force sources.
Responsibilities:
- Receives lateral force application height from debug tools
- Validates force height relative to COM and neutral band
- Emits warnings when force height would invert roll behaviour
- Stores last known force height for visualisation
- Provides a clean API for future systems needing COM‑aware validation
This closes the loop between physics forces and COM.

3. BoatCOMVisualizer
Role: Scene‑view visualiser for COM geometry.
Responsibilities:
- Draws COM position
- Draws neutral band region
- (Optional) draws force height indicators
- Provides spatial debugging for designers and engineers
This makes COM behaviour visible in 3D space.

4. BoatCOMOverlay
Role: Editor overlay for COM diagnostics.
Responsibilities:
- Displays COM height, neutral band, and related values
- Provides IMGUI controls for toggling COM offset
- Offers quick access to COM validation tools
- Anchors a floating debug panel to the COM position
- Runs only in the Unity Editor
This gives you a live COM panel without digging through inspectors.

5. LateralForceDebug
Role: Controlled roll‑stimulus generator.
Responsibilities:
- Creates a test point at a configurable height
- Applies a lateral force at that point
- Computes torque direction and magnitude
- Reports force height to BoatCOMIntegration
- Logs roll direction, torque, and force vectors
This tool is essential for validating roll behaviour without relying on rudders, waves, or motion.

## License
This repository is currently unlicensed and all rights are reserved.
No part of this codebase may be copied, modified, or redistributed without explicit permission.
