"""
Serves a Unity WebGL build the way itch.io serves it, so a local test reproduces
the real hosting behaviour rather than a generic static server's.

itch.io sets Content-Encoding purely from the file extension (.br -> br, .gz ->
gzip) and derives Content-Type from the name with that extension stripped. That
is the whole reason Decompression Fallback has to be off: with it on the files
are named .unityweb and none of this detection fires.
"""

import os
import sys
from http.server import HTTPServer, SimpleHTTPRequestHandler

ROOT = sys.argv[1]
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8099

CONTENT_TYPES = {
    ".js": "application/javascript",
    ".wasm": "application/wasm",
    ".data": "application/octet-stream",
    ".json": "application/json",
    ".html": "text/html",
    ".css": "text/css",
    ".symbols": "application/octet-stream",
}

ENCODINGS = {".br": "br", ".gz": "gzip"}


class ItchHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def end_headers(self):
        path = self.translate_path(self.path)
        _, ext = os.path.splitext(path)

        encoding = ENCODINGS.get(ext)
        if encoding:
            self.send_header("Content-Encoding", encoding)

        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        super().end_headers()

    def guess_type(self, path):
        base, ext = os.path.splitext(path)
        if ext in ENCODINGS:
            # Strip the .br/.gz and type the file by what it actually is.
            _, inner = os.path.splitext(base)
            return CONTENT_TYPES.get(inner, "application/octet-stream")
        return CONTENT_TYPES.get(ext, super().guess_type(path))

    def log_message(self, fmt, *args):
        sys.stderr.write("%s\n" % (fmt % args))


if __name__ == "__main__":
    print("Serving %s on http://localhost:%d (itch.io header emulation)" % (ROOT, PORT))
    sys.stdout.flush()
    HTTPServer(("127.0.0.1", PORT), ItchHandler).serve_forever()
