package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net"
	"os"
	"os/signal"
	"syscall"
	"time"
)

type PingConfig struct {
	Host     string `json:"host"`
	Count    int    `json:"count"`
	Interval int    `json:"interval_ms"`
	Timeout  int    `json:"timeout_ms"`
}

type PingResult struct {
	Seq       int     `json:"seq"`
	Host      string  `json:"host"`
	IP        string  `json:"ip"`
	Status    string  `json:"status"`
	LatencyMs float64 `json:"latency_ms"`
	Error     string  `json:"error,omitempty"`
}

func runPing(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg PingConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	if cfg.Count <= 0 {
		cfg.Count = 10
	}
	if cfg.Interval <= 0 {
		cfg.Interval = 1000
	}
	if cfg.Timeout <= 0 {
		cfg.Timeout = 3000
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — ping\n")
	fmt.Fprintf(os.Stderr, "  Host: %s\n", cfg.Host)
	fmt.Fprintf(os.Stderr, "  Count: %d\n", cfg.Count)
	fmt.Fprintf(os.Stderr, "  Interval: %dms\n", cfg.Interval)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		<-sigCh
		cancel()
	}()

	// Resolve host once
	ips, err := net.LookupHost(cfg.Host)
	resolvedIP := ""
	if err == nil && len(ips) > 0 {
		resolvedIP = ips[0]
	}

	for i := 1; i <= cfg.Count; i++ {
		select {
		case <-ctx.Done():
			fmt.Fprintf(os.Stderr, "Ping stopped.\n")
			return
		default:
		}

		result := PingResult{
			Seq:  i,
			Host: cfg.Host,
			IP:   resolvedIP,
		}

		// TCP ping (connect to port 80 or 443)
		start := time.Now()
		port := "80"
		if resolvedIP == "" {
			result.Status = "error"
			result.Error = "DNS resolution failed"
		} else {
			conn, err := net.DialTimeout("tcp", net.JoinHostPort(resolvedIP, port), time.Duration(cfg.Timeout)*time.Millisecond)
			elapsed := time.Since(start)
			if err != nil {
				// Try port 443
				start2 := time.Now()
				conn2, err2 := net.DialTimeout("tcp", net.JoinHostPort(resolvedIP, "443"), time.Duration(cfg.Timeout)*time.Millisecond)
				elapsed2 := time.Since(start2)
				if err2 != nil {
					result.Status = "timeout"
					result.LatencyMs = float64(elapsed.Milliseconds())
					result.Error = err.Error()
				} else {
					conn2.Close()
					result.Status = "ok"
					result.LatencyMs = float64(elapsed2.Microseconds()) / 1000.0
				}
			} else {
				conn.Close()
				result.Status = "ok"
				result.LatencyMs = float64(elapsed.Microseconds()) / 1000.0
			}
		}

		// Output JSON line to stdout
		jsonBytes, _ := json.Marshal(result)
		fmt.Println(string(jsonBytes))

		if i < cfg.Count {
			select {
			case <-ctx.Done():
				return
			case <-time.After(time.Duration(cfg.Interval) * time.Millisecond):
			}
		}
	}

	fmt.Fprintf(os.Stderr, "Ping completed.\n")
}
