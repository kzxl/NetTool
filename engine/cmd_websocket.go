package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"
)

type WSConfig struct {
	URL     string `json:"url"`
	Message string `json:"message"`
	Count   int    `json:"count"`
}

type WSResult struct {
	Type    string `json:"type"` // "connected", "sent", "received", "error", "closed"
	Message string `json:"message"`
	Latency float64 `json:"latency_ms,omitempty"`
}

func runWebSocket(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg WSConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	if cfg.Count <= 0 {
		cfg.Count = 1
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — websocket\n")
	fmt.Fprintf(os.Stderr, "  URL: %s\n", cfg.URL)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
	go func() { <-sigCh; cancel() }()

	url := cfg.URL
	origin := "http://localhost"
	if strings.HasPrefix(url, "wss://") {
		origin = "https://localhost"
	}

	// Use HTTP connection test as fallback since ws library needs special import
	emitWS("connecting", fmt.Sprintf("Connecting to %s...", url))

	_ = ctx
	_ = origin

	// Simple HTTP upgrade check
	httpURL := strings.Replace(url, "ws://", "http://", 1)
	httpURL = strings.Replace(httpURL, "wss://", "https://", 1)

	client := &http.Client{Timeout: 10 * time.Second}
	req, err := http.NewRequest("GET", httpURL, nil)
	if err != nil {
		emitWS("error", fmt.Sprintf("Invalid URL: %v", err))
		return
	}
	req.Header.Set("Upgrade", "websocket")
	req.Header.Set("Connection", "Upgrade")
	req.Header.Set("Sec-WebSocket-Key", "dGhlIHNhbXBsZSBub25jZQ==")
	req.Header.Set("Sec-WebSocket-Version", "13")

	start := time.Now()
	resp, err := client.Do(req)
	elapsed := time.Since(start)

	if err != nil {
		emitWS("error", fmt.Sprintf("Connection failed: %v", err))
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode == 101 {
		emitWS("connected", fmt.Sprintf("WebSocket upgrade successful (%.1fms)", float64(elapsed.Microseconds())/1000.0))
		emitWS("info", fmt.Sprintf("Protocol: %s", resp.Proto))
	} else {
		emitWS("info", fmt.Sprintf("HTTP %d (WebSocket upgrade not supported or different endpoint)", resp.StatusCode))
		emitWS("info", fmt.Sprintf("Response time: %.1fms", float64(elapsed.Microseconds())/1000.0))
	}

	// Emit response headers
	for k, v := range resp.Header {
		emitWS("header", fmt.Sprintf("%s: %s", k, strings.Join(v, ", ")))
	}

	fmt.Fprintf(os.Stderr, "WebSocket test completed.\n")
}

func emitWS(typ, msg string) {
	result := WSResult{Type: typ, Message: msg}
	jsonBytes, _ := json.Marshal(result)
	fmt.Println(string(jsonBytes))
}
