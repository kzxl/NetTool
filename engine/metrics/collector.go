package metrics

import (
	"sort"
	"sync"
	"time"
)

// RequestResult holds the result of a single HTTP request.
type RequestResult struct {
	Duration time.Duration
	Success  bool
	Error    string
}

// Snapshot holds aggregated metrics for a 1-second window.
type Snapshot struct {
	Time      int     `json:"t"`
	RPS       int     `json:"rps"`
	Avg       float64 `json:"avg"`
	P50       float64 `json:"p50"`
	P95       float64 `json:"p95"`
	P99       float64 `json:"p99"`
	ErrorRate float64 `json:"err"`
	Total     int     `json:"total"`
	Success   int     `json:"success"`
	Fail      int     `json:"fail"`
}

// Collector collects request results and produces periodic snapshots.
type Collector struct {
	mu       sync.Mutex
	results  []RequestResult
	resultCh chan RequestResult
	stopCh   chan struct{}
	snapCh   chan Snapshot
	tick     int
}

// NewCollector creates a new metrics collector.
func NewCollector(bufferSize int) *Collector {
	return &Collector{
		resultCh: make(chan RequestResult, bufferSize),
		stopCh:   make(chan struct{}),
		snapCh:   make(chan Snapshot, 120), // buffer enough snapshots
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

// Start begins the collection and aggregation loop.
// It aggregates results every 1 second and sends a Snapshot.
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
			// Flush remaining results
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
	// Move results out and reset slice (reuse underlying array next round)
	results := c.results
	c.results = make([]RequestResult, 0, cap(results))
	c.mu.Unlock()

	c.tick++
	snap := aggregate(c.tick, results)

	select {
	case c.snapCh <- snap:
	default:
		// drop snapshot if consumer is too slow
	}
}

func aggregate(tick int, results []RequestResult) Snapshot {
	n := len(results)
	if n == 0 {
		return Snapshot{Time: tick}
	}

	var totalDuration float64
	var successCount, failCount int

	durations := make([]float64, 0, n)

	for _, r := range results {
		ms := float64(r.Duration.Milliseconds())
		durations = append(durations, ms)
		totalDuration += ms
		if r.Success {
			successCount++
		} else {
			failCount++
		}
	}

	sort.Float64s(durations)

	snap := Snapshot{
		Time:    tick,
		RPS:     n,
		Avg:     totalDuration / float64(n),
		P50:     percentile(durations, 0.50),
		P95:     percentile(durations, 0.95),
		P99:     percentile(durations, 0.99),
		Total:   n,
		Success: successCount,
		Fail:    failCount,
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
