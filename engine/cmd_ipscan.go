package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"
)

type IPScanConfig struct {
	Subnet  string `json:"subnet"`
	Timeout int    `json:"timeout_ms"`
	Workers int    `json:"workers"`
}

type IPScanResult struct {
	IP       string `json:"ip"`
	Status   string `json:"status"`
	Latency  float64 `json:"latency_ms"`
	Hostname string `json:"hostname,omitempty"`
}

func runIPScan(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg IPScanConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	if cfg.Timeout <= 0 {
		cfg.Timeout = 1000
	}
	if cfg.Workers <= 0 {
		cfg.Workers = 50
	}

	ips := expandSubnet(cfg.Subnet)
	fmt.Fprintf(os.Stderr, "NetTool Engine — ipscan\n")
	fmt.Fprintf(os.Stderr, "  Subnet: %s\n", cfg.Subnet)
	fmt.Fprintf(os.Stderr, "  IPs to scan: %d\n", len(ips))
	fmt.Fprintf(os.Stderr, "  Workers: %d\n", cfg.Workers)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		<-sigCh
		cancel()
	}()

	ipCh := make(chan string, len(ips))
	for _, ip := range ips {
		ipCh <- ip
	}
	close(ipCh)

	var wg sync.WaitGroup
	resultCh := make(chan IPScanResult, len(ips))

	for i := 0; i < cfg.Workers; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for ip := range ipCh {
				select {
				case <-ctx.Done():
					return
				default:
				}
				result := probeIP(ip, cfg.Timeout)
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

	fmt.Fprintf(os.Stderr, "IP scan completed.\n")
}

func probeIP(ip string, timeoutMs int) IPScanResult {
	result := IPScanResult{IP: ip}

	// Try TCP connect to common ports (80, 443, 22)
	ports := []string{"80", "443", "22"}
	timeout := time.Duration(timeoutMs) * time.Millisecond

	for _, port := range ports {
		start := time.Now()
		conn, err := net.DialTimeout("tcp", net.JoinHostPort(ip, port), timeout)
		elapsed := time.Since(start)
		if err == nil {
			conn.Close()
			result.Status = "up"
			result.Latency = float64(elapsed.Microseconds()) / 1000.0

			// Try reverse DNS
			names, err := net.LookupAddr(ip)
			if err == nil && len(names) > 0 {
				result.Hostname = names[0]
			}
			return result
		}
	}

	result.Status = "down"
	return result
}

// expandSubnet takes "192.168.1.0/24" and returns all host IPs
func expandSubnet(subnet string) []string {
	_, ipnet, err := net.ParseCIDR(subnet)
	if err != nil {
		// Try single IP
		if ip := net.ParseIP(subnet); ip != nil {
			return []string{ip.String()}
		}
		return nil
	}

	var ips []string
	ip := ipnet.IP.Mask(ipnet.Mask)

	for {
		if !ipnet.Contains(ip) {
			break
		}
		// Skip network and broadcast addresses for /24+
		ipCopy := make(net.IP, len(ip))
		copy(ipCopy, ip)
		ips = append(ips, ipCopy.String())
		incrementIP(ip)
	}

	// Remove first (network) and last (broadcast) for subnets
	if len(ips) > 2 {
		ips = ips[1 : len(ips)-1]
	}

	return ips
}

func incrementIP(ip net.IP) {
	for j := len(ip) - 1; j >= 0; j-- {
		ip[j]++
		if ip[j] > 0 {
			break
		}
	}
}
