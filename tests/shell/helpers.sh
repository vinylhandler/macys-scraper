#!/bin/bash

load_mock_response() {
    cat ../shared/mock_responses.json
}

start_mock_server() {
    local port=${1:-8080}
    local response_file="../shared/mock_responses.json"
    
    python3 -c "
import http.server
import socketserver
import json
import sys
from urllib.parse import urlparse, parse_qs

class MockHandler(http.server.BaseHTTPRequestHandler):
    def do_POST(self):
        if self.path == '/v1/queries':
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            with open('$response_file', 'r') as f:
                self.wfile.write(f.read().encode())
        else:
            self.send_response(404)
            self.end_headers()
    
    def log_message(self, format, *args):
        pass  # Suppress log messages

with socketserver.TCPServer(('', $port), MockHandler) as httpd:
    httpd.serve_forever()
" &
    echo $!
}

stop_mock_server() {
    local pid=$1
    kill $pid 2>/dev/null || true
    wait $pid 2>/dev/null || true
}
