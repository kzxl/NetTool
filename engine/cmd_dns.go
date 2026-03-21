package main

import (
	"encoding/json"
	"fmt"
	"net"
	"os"
	"strings"
)

type DNSConfig struct {
	Host string `json:"host"`
}

type DNSResult struct {
	Type    string   `json:"type"`
	Records []string `json:"records"`
	Error   string   `json:"error,omitempty"`
}

func runDNS(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg DNSConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	host := strings.TrimSpace(cfg.Host)
	fmt.Fprintf(os.Stderr, "NetTool Engine — dns\n")
	fmt.Fprintf(os.Stderr, "  Host: %s\n", host)

	// A records
	emitDNS("A", func() ([]string, error) {
		ips, err := net.LookupHost(host)
		return ips, err
	})

	// AAAA (filter IPv6)
	emitDNS("AAAA", func() ([]string, error) {
		ips, err := net.LookupIP(host)
		if err != nil {
			return nil, err
		}
		var v6 []string
		for _, ip := range ips {
			if ip.To4() == nil {
				v6 = append(v6, ip.String())
			}
		}
		return v6, nil
	})

	// CNAME
	emitDNS("CNAME", func() ([]string, error) {
		cname, err := net.LookupCNAME(host)
		if err != nil {
			return nil, err
		}
		return []string{cname}, nil
	})

	// MX
	emitDNS("MX", func() ([]string, error) {
		mxs, err := net.LookupMX(host)
		if err != nil {
			return nil, err
		}
		var records []string
		for _, mx := range mxs {
			records = append(records, fmt.Sprintf("%s (priority %d)", mx.Host, mx.Pref))
		}
		return records, nil
	})

	// NS
	emitDNS("NS", func() ([]string, error) {
		nss, err := net.LookupNS(host)
		if err != nil {
			return nil, err
		}
		var records []string
		for _, ns := range nss {
			records = append(records, ns.Host)
		}
		return records, nil
	})

	// TXT
	emitDNS("TXT", func() ([]string, error) {
		return net.LookupTXT(host)
	})

	fmt.Fprintf(os.Stderr, "DNS lookup completed.\n")
}

func emitDNS(recordType string, lookup func() ([]string, error)) {
	records, err := lookup()
	result := DNSResult{Type: recordType}
	if err != nil {
		result.Error = err.Error()
		result.Records = []string{}
	} else {
		result.Records = records
		if result.Records == nil {
			result.Records = []string{}
		}
	}
	jsonBytes, _ := json.Marshal(result)
	fmt.Println(string(jsonBytes))
}
