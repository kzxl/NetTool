package config

import (
	"encoding/json"
	"fmt"
	"os"
)

// Config represents the full test configuration.
type Config struct {
	Request   RequestConfig `json:"request"`
	Load      LoadConfig    `json:"load"`
	TimeoutMs int           `json:"timeoutMs"`
}

// RequestConfig holds HTTP request parameters.
type RequestConfig struct {
	URL     string            `json:"url"`
	Method  string            `json:"method"`
	Headers map[string]string `json:"headers"`
	Body    string            `json:"body"`
}

// LoadConfig holds load test parameters.
type LoadConfig struct {
	Concurrency int `json:"concurrency"`
	DurationSec int `json:"durationSec"`
	RampUpSec   int `json:"rampUpSec"`
}

// Validate checks if the configuration is valid.
func (c *Config) Validate() error {
	if c.Request.URL == "" {
		return fmt.Errorf("request.url is required")
	}
	if c.Request.Method == "" {
		c.Request.Method = "GET"
	}
	if c.Load.Concurrency <= 0 {
		return fmt.Errorf("load.concurrency must be > 0")
	}
	if c.Load.DurationSec <= 0 {
		return fmt.Errorf("load.durationSec must be > 0")
	}
	if c.TimeoutMs <= 0 {
		c.TimeoutMs = 5000
	}
	return nil
}

// LoadFromFile reads and parses a JSON config file.
func LoadFromFile(path string) (*Config, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("failed to read config file: %w", err)
	}

	var cfg Config
	if err := json.Unmarshal(data, &cfg); err != nil {
		return nil, fmt.Errorf("failed to parse config: %w", err)
	}

	if err := cfg.Validate(); err != nil {
		return nil, fmt.Errorf("invalid config: %w", err)
	}

	return &cfg, nil
}
