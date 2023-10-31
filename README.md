# Lunatico AAG CloudWatcher Prometheus Exporter
Prometheus exporter for the Lunatico AAG CloudWatcher.

# Configuration

Set the `AagDirectory` environment variable to the directory that contains:

  - `aag_json.dat`
  - `DebugData.txt`

# Usage

Metrics are available on port 9100 at `/metrics`.

Additionally, there are two HTTP endpoints on port 80:

  - `/aag`
  - `/debug`