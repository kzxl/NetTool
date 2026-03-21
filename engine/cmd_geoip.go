package main

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"time"
)

type GeoIPConfig struct {
	IP string `json:"ip"`
}

type GeoIPResult struct {
	Key   string `json:"key"`
	Value string `json:"value"`
}

func runGeoIP(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg GeoIPConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — geoip\n")
	fmt.Fprintf(os.Stderr, "  IP: %s\n", cfg.IP)

	// Use free ip-api.com
	url := fmt.Sprintf("http://ip-api.com/json/%s?fields=status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,org,as,query", cfg.IP)

	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(url)
	if err != nil {
		emitGeo("Error", err.Error())
		return
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(resp.Body)

	var result map[string]interface{}
	if err := json.Unmarshal(body, &result); err != nil {
		emitGeo("Error", "Failed to parse response")
		return
	}

	fields := []struct{ key, label string }{
		{"query", "IP Address"},
		{"country", "Country"},
		{"countryCode", "Country Code"},
		{"regionName", "Region"},
		{"city", "City"},
		{"zip", "ZIP Code"},
		{"lat", "Latitude"},
		{"lon", "Longitude"},
		{"timezone", "Timezone"},
		{"isp", "ISP"},
		{"org", "Organization"},
		{"as", "AS Number"},
	}

	for _, f := range fields {
		if v, ok := result[f.key]; ok && v != nil {
			emitGeo(f.label, fmt.Sprintf("%v", v))
		}
	}

	fmt.Fprintf(os.Stderr, "GeoIP lookup completed.\n")
}

func emitGeo(key, value string) {
	result := GeoIPResult{Key: key, Value: value}
	jsonBytes, _ := json.Marshal(result)
	fmt.Println(string(jsonBytes))
}
