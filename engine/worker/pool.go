package worker

import (
	"context"
	"io"
	"net/http"
	"strings"
	"time"

	"nettool/engine/config"
	"nettool/engine/metrics"
)

// Pool manages a pool of workers that send HTTP requests.
type Pool struct {
	cfg      *config.Config
	client   *http.Client
	resultCh chan<- metrics.RequestResult
}

// NewPool creates a new worker pool.
func NewPool(cfg *config.Config, resultCh chan<- metrics.RequestResult) *Pool {
	transport := &http.Transport{
		MaxIdleConns:        cfg.Load.Concurrency + 50,
		MaxIdleConnsPerHost: cfg.Load.Concurrency + 50,
		IdleConnTimeout:     90 * time.Second,
		DisableKeepAlives:   false,
	}

	client := &http.Client{
		Timeout:   time.Duration(cfg.TimeoutMs) * time.Millisecond,
		Transport: transport,
	}

	return &Pool{
		cfg:      cfg,
		client:   client,
		resultCh: resultCh,
	}
}

// Run starts sending requests with the configured concurrency and ramp-up.
// It blocks until the context is cancelled or the duration expires.
func (p *Pool) Run(ctx context.Context) {
	concurrency := p.cfg.Load.Concurrency
	rampUpSec := p.cfg.Load.RampUpSec
	durationSec := p.cfg.Load.DurationSec

	// Create a context with the total duration timeout
	ctx, cancel := context.WithTimeout(ctx, time.Duration(durationSec)*time.Second)
	defer cancel()

	sem := make(chan struct{}, concurrency)

	// Calculate ramp-up: gradually increase workers
	rampInterval := time.Duration(0)
	if rampUpSec > 0 && concurrency > 1 {
		rampInterval = time.Duration(rampUpSec) * time.Second / time.Duration(concurrency)
	}

	currentMax := 1
	rampTicker := time.NewTicker(max(rampInterval, 1*time.Millisecond))
	defer rampTicker.Stop()

	// Ramp-up goroutine
	if rampUpSec > 0 && concurrency > 1 {
		go func() {
			for {
				select {
				case <-ctx.Done():
					return
				case <-rampTicker.C:
					if currentMax < concurrency {
						currentMax++
					} else {
						return
					}
				}
			}
		}()
	} else {
		currentMax = concurrency
	}

	// Main request loop
	for {
		select {
		case <-ctx.Done():
			// Wait for in-flight requests to finish
			for i := 0; i < cap(sem); i++ {
				sem <- struct{}{}
			}
			return
		default:
			// Check ramp-up limit: only allow up to currentMax concurrent
			if len(sem) >= currentMax {
				// Wait a bit before retrying
				time.Sleep(1 * time.Millisecond)
				continue
			}

			sem <- struct{}{}
			go func() {
				defer func() { <-sem }()
				p.doRequest(ctx)
			}()
		}
	}
}

func (p *Pool) doRequest(ctx context.Context) {
	reqCfg := p.cfg.Request

	var body io.Reader
	if reqCfg.Body != "" {
		body = strings.NewReader(reqCfg.Body)
	}

	req, err := http.NewRequestWithContext(ctx, reqCfg.Method, reqCfg.URL, body)
	if err != nil {
		p.resultCh <- metrics.RequestResult{
			Duration: 0,
			Success:  false,
			Error:    err.Error(),
		}
		return
	}

	for k, v := range reqCfg.Headers {
		req.Header.Set(k, v)
	}

	start := time.Now()
	resp, err := p.client.Do(req)
	duration := time.Since(start)

	if err != nil {
		// Don't report context cancelled errors during shutdown
		if ctx.Err() != nil {
			return
		}
		p.resultCh <- metrics.RequestResult{
			Duration: duration,
			Success:  false,
			Error:    err.Error(),
		}
		return
	}

	// Drain and close body to allow connection reuse
	io.Copy(io.Discard, resp.Body)
	resp.Body.Close()

	success := resp.StatusCode >= 200 && resp.StatusCode < 400

	p.resultCh <- metrics.RequestResult{
		Duration: duration,
		Success:  success,
	}
}

func max(a, b time.Duration) time.Duration {
	if a > b {
		return a
	}
	return b
}
