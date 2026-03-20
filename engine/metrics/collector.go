package metrics

import (
	"math"
	"sort"
	"sync"
	"sync/atomic"
	"time"
)

// RequestResult holds the result of a single HTTP request.
type RequestResult struct {
	Duration      time.Duration
	StatusCode    int
	BytesReceived int64
	Success       bool
	Error         string
}

// StatusCodeDist holds the distribution of HTTP status codes.
type StatusCodeDist struct {
	S2xx int `json:"2xx"`
	S3xx int `json:"3xx"`
	S4xx int `json:"4xx"`
	S5xx int `json:"5xx"`
}

// Snapshot holds aggregated metrics for a 1-second window.
type Snapshot struct {
	Time          int            `json:"t"`
	RPS           int            `json:"rps"`
	Avg           float64        `json:"avg"`
	Min           float64        `json:"min"`
	Max           float64        `json:"max"`
	StdDev        float64        `json:"stddev"`
	P50           float64        `json:"p50"`
	P95           float64        `json:"p95"`
	P99           float64        `json:"p99"`
	ErrorRate     float64        `json:"err"`
	Total         int            `json:"total"`
	Success       int            `json:"success"`
	Fail          int            `json:"fail"`
	StatusCodes   StatusCodeDist `json:"codes"`
	BytesReceived int64          `json:"bytes"`
	ActiveConns   int64          `json:"conns"`
}

// Collector collects request results and produces periodic snapshots.
type Collector struct {
	mu          sync.Mutex
	results     []RequestResult
	resultCh    chan RequestResult
	stopCh      chan struct{}
	snapCh      chan Snapshot
	tick        int
	ActiveConns int64 // atomic: current in-flight requests
}

// NewCollector creates a new metrics collector.
func NewCollector(bufferSize int) *Collector {
	return &Collector{
		resultCh: make(chan RequestResult, bufferSize),
		stopCh:   make(chan struct{}),
		snapCh:   make(chan Snapshot, 120),
	}
}

// ResultChan returns the channel to send request results to.
func (c *Collector) ResultChan() chan<- RequestResult {
	return c.resultCh
}

// SnapshotChan returns the channel that emits aggregated snapshots.
func (c *Collector) SnapshotChan() <-chan Snapshot {
	return c.snapCh
}

// IncrConns atomically increments the active connection count.
func (c *Collector) IncrConns() {
	atomic.AddInt64(&c.ActiveConns, 1)
}

// DecrConns atomically decrements the active connection count.
func (c *Collector) DecrConns() {
	atomic.AddInt64(&c.ActiveConns, -1)
}

// Start begins the collection and aggregation loop.
func (c *Collector) Start() {
	go c.collectLoop()
}

// Stop signals the collector to stop.
func (c *Collector) Stop() {
	close(c.stopCh)
}

func (c *Collector) collectLoop() {
	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()
	defer close(c.snapCh)

	for {
		select {
		case <-c.stopCh:
			c.flush()
			return

		case result := <-c.resultCh:
			c.mu.Lock()
			c.results = append(c.results, result)
			c.mu.Unlock()

		case <-ticker.C:
			c.flush()
		}
	}
}

func (c *Collector) flush() {
	c.mu.Lock()
	if len(c.results) == 0 {
		c.mu.Unlock()
		return
	}
	results := c.results
	c.results = make([]RequestResult, 0, cap(results))
	c.mu.Unlock()

	c.tick++
	activeConns := atomic.LoadInt64(&c.ActiveConns)
	snap := aggregate(c.tick, results, activeConns)

	select {
	case c.snapCh <- snap:
	default:
	}
}

func aggregate(tick int, results []RequestResult, activeConns int64) Snapshot {
	n := len(results)
	if n == 0 {
		return Snapshot{Time: tick, ActiveConns: activeConns}
	}

	var totalDuration float64
	var successCount, failCount int
	var totalBytes int64
	var codes StatusCodeDist

	durations := make([]float64, 0, n)

	for _, r := range results {
		ms := float64(r.Duration.Milliseconds())
		durations = append(durations, ms)
		totalDuration += ms
		totalBytes += r.BytesReceived

		if r.Success {
			successCount++
		} else {
			failCount++
		}

		// Status code distribution
		switch {
		case r.StatusCode >= 200 && r.StatusCode < 300:
			codes.S2xx++
		case r.StatusCode >= 300 && r.StatusCode < 400:
			codes.S3xx++
		case r.StatusCode >= 400 && r.StatusCode < 500:
			codes.S4xx++
		case r.StatusCode >= 500:
			codes.S5xx++
		}
	}

	sort.Float64s(durations)

	avg := totalDuration / float64(n)

	// Standard deviation
	var sumSqDiff float64
	for _, d := range durations {
		diff := d - avg
		sumSqDiff += diff * diff
	}
	stddev := math.Sqrt(sumSqDiff / float64(n))

	snap := Snapshot{
		Time:          tick,
		RPS:           n,
		Avg:           avg,
		Min:           durations[0],
		Max:           durations[n-1],
		StdDev:        stddev,
		P50:           percentile(durations, 0.50),
		P95:           percentile(durations, 0.95),
		P99:           percentile(durations, 0.99),
		Total:         n,
		Success:       successCount,
		Fail:          failCount,
		StatusCodes:   codes,
		BytesReceived: totalBytes,
		ActiveConns:   activeConns,
	}

	if n > 0 {
		snap.ErrorRate = float64(failCount) / float64(n)
	}

	return snap
}

func percentile(sorted []float64, p float64) float64 {
	if len(sorted) == 0 {
		return 0
	}
	idx := int(float64(len(sorted)-1) * p)
	if idx >= len(sorted) {
		idx = len(sorted) - 1
	}
	return sorted[idx]
}
