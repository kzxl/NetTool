package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net"
	"os"
	"os/signal"
	"sort"
	"sync"
	"syscall"
	"time"
)

type PortScanConfig struct {
	Host    string `json:"host"`
	Ports   string `json:"ports"`
	Timeout int    `json:"timeout_ms"`
	Workers int    `json:"workers"`
}

type PortResult struct {
	Port    int    `json:"port"`
	Status  string `json:"status"`
	Service string `json:"service"`
	Latency float64 `json:"latency_ms"`
}

var commonPorts = map[int]string{
	21: "FTP", 22: "SSH", 23: "Telnet", 25: "SMTP", 53: "DNS",
	80: "HTTP", 110: "POP3", 143: "IMAP", 443: "HTTPS", 445: "SMB",
	993: "IMAPS", 995: "POP3S", 1433: "MSSQL", 1521: "Oracle",
	3306: "MySQL", 3389: "RDP", 5432: "PostgreSQL", 5900: "VNC",
	6379: "Redis", 8080: "HTTP-Alt", 8443: "HTTPS-Alt", 27017: "MongoDB",
}

func runPortScan(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg PortScanConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	if cfg.Timeout <= 0 {
		cfg.Timeout = 2000
	}
	if cfg.Workers <= 0 {
		cfg.Workers = 50
	}

	ports := parsePorts(cfg.Ports)
	fmt.Fprintf(os.Stderr, "NetTool Engine — portscan\n")
	fmt.Fprintf(os.Stderr, "  Host: %s\n", cfg.Host)
	fmt.Fprintf(os.Stderr, "  Ports: %d\n", len(ports))
	fmt.Fprintf(os.Stderr, "  Workers: %d\n", cfg.Workers)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		<-sigCh
		cancel()
	}()

	portCh := make(chan int, len(ports))
	for _, p := range ports {
		portCh <- p
	}
	close(portCh)

	var wg sync.WaitGroup
	resultCh := make(chan PortResult, len(ports))

	for i := 0; i < cfg.Workers; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for port := range portCh {
				select {
				case <-ctx.Done():
					return
				default:
				}
				result := scanPort(cfg.Host, port, cfg.Timeout)
				resultCh <- result
			}
		}()
	}

	go func() {
		wg.Wait()
		close(resultCh)
	}()

	for result := range resultCh {
		jsonBytes, _ := json.Marshal(result)
		fmt.Println(string(jsonBytes))
	}

	fmt.Fprintf(os.Stderr, "Port scan completed.\n")
}

func scanPort(host string, port, timeoutMs int) PortResult {
	addr := fmt.Sprintf("%s:%d", host, port)
	start := time.Now()
	conn, err := net.DialTimeout("tcp", addr, time.Duration(timeoutMs)*time.Millisecond)
	elapsed := time.Since(start)

	result := PortResult{
		Port:    port,
		Latency: float64(elapsed.Microseconds()) / 1000.0,
	}

	if err != nil {
		result.Status = "closed"
	} else {
		conn.Close()
		result.Status = "open"
	}

	if svc, ok := commonPorts[port]; ok {
		result.Service = svc
	}

	return result
}

func parsePorts(spec string) []int {
	if spec == "" || spec == "common" {
		keys := make([]int, 0, len(commonPorts))
		for k := range commonPorts {
			keys = append(keys, k)
		}
		sort.Ints(keys)
		return keys
	}

	// Parse: "80,443,1000-2000"
	var ports []int
	for _, part := range splitComma(spec) {
		if dashIdx := indexOf(part, '-'); dashIdx >= 0 {
			var lo, hi int
			fmt.Sscanf(part[:dashIdx], "%d", &lo)
			fmt.Sscanf(part[dashIdx+1:], "%d", &hi)
			for p := lo; p <= hi && p <= 65535; p++ {
				ports = append(ports, p)
			}
		} else {
			var p int
			fmt.Sscanf(part, "%d", &p)
			if p > 0 && p <= 65535 {
				ports = append(ports, p)
			}
		}
	}
	sort.Ints(ports)
	return ports
}

func splitComma(s string) []string {
	var parts []string
	for _, p := range splitBy(s, ',') {
		trimmed := trimSpace(p)
		if trimmed != "" {
			parts = append(parts, trimmed)
		}
	}
	return parts
}

func splitBy(s string, sep byte) []string {
	var result []string
	start := 0
	for i := 0; i < len(s); i++ {
		if s[i] == sep {
			result = append(result, s[start:i])
			start = i + 1
		}
	}
	result = append(result, s[start:])
	return result
}

func indexOf(s string, c byte) int {
	for i := 0; i < len(s); i++ {
		if s[i] == c {
			return i
		}
	}
	return -1
}

func trimSpace(s string) string {
	start, end := 0, len(s)
	for start < end && (s[start] == ' ' || s[start] == '\t') {
		start++
	}
	for end > start && (s[end-1] == ' ' || s[end-1] == '\t') {
		end--
	}
	return s[start:end]
}
