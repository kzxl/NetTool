package main

import (
	"crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"sort"
	"strings"
	"time"
)

type HTTPHeadersConfig struct {
	URL     string `json:"url"`
	Method  string `json:"method"`
	Timeout int    `json:"timeout_ms"`
}

type HeaderResult struct {
	Type       string `json:"type"` // "status", "header", "tls"
	Key        string `json:"key"`
	Value      string `json:"value"`
}

func runHTTPHeaders(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg HTTPHeadersConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	if cfg.Method == "" {
		cfg.Method = "GET"
	}
	if cfg.Timeout <= 0 {
		cfg.Timeout = 10000
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — httpheaders\n")
	fmt.Fprintf(os.Stderr, "  URL: %s\n", cfg.URL)

	url := cfg.URL
	if !strings.HasPrefix(url, "http://") && !strings.HasPrefix(url, "https://") {
		url = "https://" + url
	}

	client := &http.Client{
		Timeout: time.Duration(cfg.Timeout) * time.Millisecond,
		CheckRedirect: func(req *http.Request, via []*http.Request) error {
			return http.ErrUseLastResponse
		},
	}

	req, err := http.NewRequest(strings.ToUpper(cfg.Method), url, nil)
	if err != nil {
		emitHeader("error", "Error", err.Error())
		return
	}
	req.Header.Set("User-Agent", "NetTool/1.0")

	start := time.Now()
	resp, err := client.Do(req)
	elapsed := time.Since(start)
	if err != nil {
		emitHeader("error", "Error", err.Error())
		return
	}
	defer resp.Body.Close()
	io.ReadAll(resp.Body)

	// Status
	emitHeader("status", "Status", fmt.Sprintf("%d %s", resp.StatusCode, resp.Status))
	emitHeader("status", "Response Time", fmt.Sprintf("%.1f ms", float64(elapsed.Microseconds())/1000.0))
	emitHeader("status", "Protocol", resp.Proto)

	// TLS info
	if resp.TLS != nil {
		emitHeader("tls", "TLS Version", tlsVersionName(resp.TLS.Version))
		emitHeader("tls", "Cipher Suite", tls.CipherSuiteName(resp.TLS.CipherSuite))
		if len(resp.TLS.PeerCertificates) > 0 {
			cert := resp.TLS.PeerCertificates[0]
			emitHeader("tls", "Certificate", cert.Subject.CommonName)
			emitHeader("tls", "Issuer", cert.Issuer.CommonName)
			emitHeader("tls", "Expires", cert.NotAfter.Format("2006-01-02"))
		}
	}

	// Headers sorted
	keys := make([]string, 0, len(resp.Header))
	for k := range resp.Header {
		keys = append(keys, k)
	}
	sort.Strings(keys)

	for _, k := range keys {
		emitHeader("header", k, strings.Join(resp.Header[k], ", "))
	}

	fmt.Fprintf(os.Stderr, "HTTP headers completed.\n")
}

func emitHeader(typ, key, value string) {
	result := HeaderResult{Type: typ, Key: key, Value: value}
	jsonBytes, _ := json.Marshal(result)
	fmt.Println(string(jsonBytes))
}

func tlsVersionName(v uint16) string {
	switch v {
	case tls.VersionTLS10:
		return "TLS 1.0"
	case tls.VersionTLS11:
		return "TLS 1.1"
	case tls.VersionTLS12:
		return "TLS 1.2"
	case tls.VersionTLS13:
		return "TLS 1.3"
	default:
		return fmt.Sprintf("0x%04x", v)
	}
}
