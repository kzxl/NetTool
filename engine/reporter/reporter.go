package reporter

import (
	"encoding/json"
	"fmt"
	"os"

	"nettool/engine/metrics"
)

// Reporter writes metric snapshots as JSON lines to stdout.
type Reporter struct{}

// NewReporter creates a new Reporter.
func NewReporter() *Reporter {
	return &Reporter{}
}

// Report writes a single snapshot as a JSON line to stdout.
func (r *Reporter) Report(snap metrics.Snapshot) {
	data, err := json.Marshal(snap)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error marshaling snapshot: %v\n", err)
		return
	}
	fmt.Println(string(data))
}
