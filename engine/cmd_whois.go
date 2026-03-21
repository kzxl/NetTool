package main

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"strings"
	"time"
)

type WhoisConfig struct {
	Domain string `json:"domain"`
}

type WhoisResult struct {
	Key   string `json:"key"`
	Value string `json:"value"`
}

func runWhois(configPath string) {
	data, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error reading config: %v\n", err)
		os.Exit(1)
	}

	var cfg WhoisConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		fmt.Fprintf(os.Stderr, "Error parsing config: %v\n", err)
		os.Exit(1)
	}

	fmt.Fprintf(os.Stderr, "NetTool Engine — whois\n")
	fmt.Fprintf(os.Stderr, "  Domain: %s\n", cfg.Domain)

	// Use RDAP (modern WHOIS replacement) via rdap.org
	url := fmt.Sprintf("https://rdap.org/domain/%s", cfg.Domain)

	client := &http.Client{Timeout: 15 * time.Second}
	req, _ := http.NewRequest("GET", url, nil)
	req.Header.Set("Accept", "application/rdap+json")

	resp, err := client.Do(req)
	if err != nil {
		emitWhois("Error", err.Error())
		return
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(resp.Body)

	var result map[string]interface{}
	if err := json.Unmarshal(body, &result); err != nil {
		emitWhois("Error", "Failed to parse RDAP response")
		return
	}

	// Extract key fields
	if name, ok := result["ldhName"].(string); ok {
		emitWhois("Domain Name", strings.ToUpper(name))
	}

	if status, ok := result["status"].([]interface{}); ok {
		var statuses []string
		for _, s := range status {
			statuses = append(statuses, fmt.Sprintf("%v", s))
		}
		emitWhois("Status", strings.Join(statuses, ", "))
	}

	// Events (registration, expiration, last changed)
	if events, ok := result["events"].([]interface{}); ok {
		for _, e := range events {
			ev, ok := e.(map[string]interface{})
			if !ok {
				continue
			}
			action := fmt.Sprintf("%v", ev["eventAction"])
			date := fmt.Sprintf("%v", ev["eventDate"])
			if len(date) > 10 {
				date = date[:10]
			}
			switch action {
			case "registration":
				emitWhois("Registered", date)
			case "expiration":
				emitWhois("Expires", date)
			case "last changed":
				emitWhois("Last Updated", date)
			}
		}
	}

	// Nameservers
	if ns, ok := result["nameservers"].([]interface{}); ok {
		var names []string
		for _, n := range ns {
			if nMap, ok := n.(map[string]interface{}); ok {
				if name, ok := nMap["ldhName"].(string); ok {
					names = append(names, name)
				}
			}
		}
		if len(names) > 0 {
			emitWhois("Nameservers", strings.Join(names, ", "))
		}
	}

	// Entities (registrar, registrant)
	if entities, ok := result["entities"].([]interface{}); ok {
		for _, e := range entities {
			ent, ok := e.(map[string]interface{})
			if !ok {
				continue
			}
			roles, _ := ent["roles"].([]interface{})
			handle, _ := ent["handle"].(string)

			for _, r := range roles {
				role := fmt.Sprintf("%v", r)
				if role == "registrar" {
					// Try to get name from vcardArray
					name := extractVcardName(ent)
					if name == "" {
						name = handle
					}
					emitWhois("Registrar", name)
				}
			}
		}
	}

	fmt.Fprintf(os.Stderr, "Whois lookup completed.\n")
}

func extractVcardName(entity map[string]interface{}) string {
	vcardArray, ok := entity["vcardArray"].([]interface{})
	if !ok || len(vcardArray) < 2 {
		return ""
	}
	cards, ok := vcardArray[1].([]interface{})
	if !ok {
		return ""
	}
	for _, card := range cards {
		arr, ok := card.([]interface{})
		if !ok || len(arr) < 4 {
			continue
		}
		if fmt.Sprintf("%v", arr[0]) == "fn" {
			return fmt.Sprintf("%v", arr[3])
		}
	}
	return ""
}

func emitWhois(key, value string) {
	result := WhoisResult{Key: key, Value: value}
	jsonBytes, _ := json.Marshal(result)
	fmt.Println(string(jsonBytes))
}
