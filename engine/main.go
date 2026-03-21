package main

import (
	"context"
	"fmt"
	"os"
	"os/signal"
	"sync"
	"syscall"

	"nettool/engine/config"
	"nettool/engine/metrics"
	"nettool/engine/reporter"
	"nettool/engine/worker"
)

func main() {
	if len(os.Args) < 2 {
		fmt.Fprintf(os.Stderr, "Usage: engine <command> <config.json>\n")
		fmt.Fprintf(os.Stderr, "Commands: loadtest\n")
		os.Exit(1)
	}

	// Multi-command dispatch
	// Backward-compatible: nếu arg[1] là file .json → default loadtest
	command := os.Args[1]
	configPath := ""

	if len(os.Args) >= 3 {
		configPath = os.Args[2]
	} else if isConfigFile(command) {
		// Backward-compatible: engine.exe config.json → default loadtest
		configPath = command
		command = "loadtest"
	} else {
		fmt.Fprintf(os.Stderr, "Error: config file path required\n")
		fmt.Fprintf(os.Stderr, "Usage: engine %s <config.json>\n", command)
		os.Exit(1)
	}

	switch command {
	case "loadtest":
		runLoadTest(configPath)
	case "ping":
		runPing(configPath)
	case "dns":
		runDNS(configPath)
	case "portscan":
		runPortScan(configPath)
	case "ipscan":
		runIPScan(configPath)
	case "httpheaders":
		runHTTPHeaders(configPath)
	default:
		fmt.Fprintf(os.Stderr, "Unknown command: %s\n", command)
		fmt.Fprintf(os.Stderr, "Available commands: loadtest, ping, dns, portscan, ipscan, httpheaders\n")
		os.Exit(1)
	}
}

// isConfigFile checks if the argument looks like a config file path
func isConfigFile(arg string) bool {
	n := len(arg)
	return n > 5 && arg[n-5:] == ".json"
}

func runLoadTest(configPath string) {
	// Load config
	cfg, err := config.LoadFromFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error: %v\n", err)
		os.Exit(1)
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — loadtest\n")
	fmt.Fprintf(os.Stderr, "  URL: %s %s\n", cfg.Request.Method, cfg.Request.URL)
	fmt.Fprintf(os.Stderr, "  Concurrency: %d\n", cfg.Load.Concurrency)
	fmt.Fprintf(os.Stderr, "  Duration: %ds\n", cfg.Load.DurationSec)
	fmt.Fprintf(os.Stderr, "  Ramp-up: %ds\n", cfg.Load.RampUpSec)
	fmt.Fprintf(os.Stderr, "  Timeout: %dms\n", cfg.TimeoutMs)

	// Setup context with signal handling
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		<-sigCh
		fmt.Fprintf(os.Stderr, "\nReceived stop signal, shutting down...\n")
		cancel()
	}()

	// Setup metrics collector
	bufferSize := cfg.Load.Concurrency * 10
	if bufferSize < 1000 {
		bufferSize = 1000
	}
	collector := metrics.NewCollector(bufferSize)
	collector.Start()

	// Setup reporter
	rep := reporter.NewReporter()

	// Consume snapshots in a goroutine
	var wg sync.WaitGroup
	wg.Add(1)
	go func() {
		defer wg.Done()
		for snap := range collector.SnapshotChan() {
			rep.Report(snap)
		}
	}()

	// Run worker pool (blocks until done or cancelled)
	pool := worker.NewPool(cfg, collector)
	pool.Run(ctx)

	// Stop collector and wait for final snapshot
	collector.Stop()
	wg.Wait()

	fmt.Fprintf(os.Stderr, "Test completed.\n")
}
