# EMVR Runtime

Runtime owns shared session and lab runtime context services.

Responsibilities:

- Session identity and lab context.
- Object registration and lookup.
- Service initialization order.
- Runtime state needed by Assist, Pedagogy, Telemetry, and Labs.

Runtime should not contain electrostatics-specific teaching logic.

