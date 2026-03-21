package main

import (
	"crypto/tls"
	"encoding/json"
	"fmt"
	"net"
	"os"
	"strings"
	"time"
)

type SSLConfig struct {
	Host    string `json:"host"`
	Port    int    `json:"port"`
	Timeout int    `json:"timeout_ms"`
}

type SSLResult struct {
	Type  string `json:"type"`
	Key   string `json:"key"`
	Value string `json:"value"`
}

func runSSL(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg SSLConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	if cfg.Port <= 0 {
		cfg.Port = 443
	}
	if cfg.Timeout <= 0 {
		cfg.Timeout = 10000
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — ssl\n")
	fmt.Fprintf(os.Stderr, "  Host: %s:%d\n", cfg.Host, cfg.Port)

	addr := fmt.Sprintf("%s:%d", cfg.Host, cfg.Port)
	dialer := &net.Dialer{Timeout: time.Duration(cfg.Timeout) * time.Millisecond}

	conn, err := tls.DialWithDialer(dialer, "tcp", addr, &tls.Config{
		InsecureSkipVerify: false,
		ServerName:         cfg.Host,
	})
	if err != nil {
		// Try with InsecureSkipVerify
		conn, err = tls.DialWithDialer(dialer, "tcp", addr, &tls.Config{
			InsecureSkipVerify: true,
			ServerName:         cfg.Host,
		})
		if err != nil {
			emitSSL("error", "Connection Error", err.Error())
			return
		}
		emitSSL("warning", "Certificate Validation", "FAILED — insecure connection")
	} else {
		emitSSL("status", "Certificate Validation", "✅ PASSED")
	}
	defer conn.Close()

	state := conn.ConnectionState()

	// TLS info
	emitSSL("tls", "TLS Version", tlsVersionName(state.Version))
	emitSSL("tls", "Cipher Suite", tls.CipherSuiteName(state.CipherSuite))
	emitSSL("tls", "Server Name", state.ServerName)

	// Certificate chain
	for i, cert := range state.PeerCertificates {
		prefix := "cert"
		if i == 0 {
			prefix = "leaf"
			emitSSL(prefix, "Subject", cert.Subject.CommonName)
			emitSSL(prefix, "Issuer", cert.Issuer.CommonName)
			emitSSL(prefix, "Valid From", cert.NotBefore.Format("2006-01-02 15:04:05"))
			emitSSL(prefix, "Valid Until", cert.NotAfter.Format("2006-01-02 15:04:05"))

			daysLeft := int(time.Until(cert.NotAfter).Hours() / 24)
			status := "✅ Valid"
			if daysLeft < 0 {
				status = "❌ EXPIRED"
			} else if daysLeft < 30 {
				status = fmt.Sprintf("⚠️ Expiring soon (%d days)", daysLeft)
			} else {
				status = fmt.Sprintf("✅ Valid (%d days remaining)", daysLeft)
			}
			emitSSL(prefix, "Expiry Status", status)

			emitSSL(prefix, "Serial Number", fmt.Sprintf("%X", cert.SerialNumber))
			emitSSL(prefix, "Signature Algorithm", cert.SignatureAlgorithm.String())

			if len(cert.DNSNames) > 0 {
				emitSSL(prefix, "DNS Names", strings.Join(cert.DNSNames, ", "))
			}
		} else {
			emitSSL("chain", fmt.Sprintf("CA #%d", i), cert.Subject.CommonName)
		}
	}

	fmt.Fprintf(os.Stderr, "SSL check completed.\n")
}

func emitSSL(typ, key, value string) {
	result := SSLResult{Type: typ, Key: key, Value: value}
	jsonBytes, _ := json.Marshal(result)
	fmt.Println(string(jsonBytes))
}
